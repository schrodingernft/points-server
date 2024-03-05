using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Microsoft.Extensions.Options;
using Orleans;
using Points.Contracts.Point;
using PointsServer.Apply.Dtos;
using PointsServer.Apply.Etos;
using PointsServer.Common;
using PointsServer.Common.AElfSdk;
using PointsServer.DApps.Provider;
using PointsServer.Grains.Grain.Operator;
using PointsServer.Options;
using PointsServer.Users.Provider;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace PointsServer.Apply;

public class ApplyService : PointsPlatformAppService, IApplyService
{
    private readonly IOperatorDomainProvider _operatorDomainProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IContractProvider _contractProvider;
    private readonly ApplyConfirmOptions _applyConfirmOptions;

    public ApplyService(IOperatorDomainProvider operatorDomainProvider, IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, IObjectMapper objectMapper,
        IUserInformationProvider userInformationProvider, IContractProvider contractProvider,
        IOptionsSnapshot<ApplyConfirmOptions> applyConfirmOptions)
    {
        _operatorDomainProvider = operatorDomainProvider;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _userInformationProvider = userInformationProvider;
        _contractProvider = contractProvider;
        _applyConfirmOptions = applyConfirmOptions.Value;
    }

    public async Task<ApplyCheckResultDto> ApplyCheckAsync(ApplyCheckInput input)
    {
        var result = new ApplyCheckResultDto();
        if (!IsValidDomain(input.Domain))
        {
            result.DomainCheck = "invalid domain format";
        }

        if (await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain) != null)
        {
            result.DomainCheck = "this domain already existed";
        }

        return result;
    }

    public async Task<ApplyConfirmDto> ApplyConfirmAsync(ApplyConfirmInput input)
    {
        var transaction =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(input.RawTransaction));

        if (!VerifyHelper.VerifySignature(transaction, input.PublicKey))
        {
            throw new UserFriendlyException("RawTransaction validation failed");
        }

        var applyToOperatorInput = new ApplyToBeAdvocateInput();
        if (transaction.To.ToBase58() == _applyConfirmOptions.CAContractAddress &&
            transaction.MethodName == "ManagerForwardCall")
        {
            var managerForwardCallInput = ManagerForwardCallInput.Parser.ParseFrom(transaction.Params);
            if (managerForwardCallInput.MethodName == "ApplyToBeAdvocate" &&
                managerForwardCallInput.ContractAddress.ToBase58() == _applyConfirmOptions.PointContractAddress)
            {
                applyToOperatorInput = ApplyToBeAdvocateInput.Parser.ParseFrom(managerForwardCallInput.Args);
            }
        }
        else if (transaction.To.ToBase58() == _applyConfirmOptions.PointContractAddress &&
                 transaction.MethodName == "ApplyToBeAdvocate")
        {
            applyToOperatorInput = ApplyToBeAdvocateInput.Parser.ParseFrom(transaction.Params);
        }
        else
        {
            throw new UserFriendlyException("Invalid transaction");
        }

        var userInfo = await _userInformationProvider.GetUserById(CurrentUser.GetId());

        var dto = new OperatorDomainGrainDto()
        {
            Address = applyToOperatorInput.Invitee.ToBase58(),
            InviterAddress = userInfo.CaAddressMain,
            Role = applyToOperatorInput.Invitee.ToBase58() == userInfo.CaAddressMain
                ? OperatorRole.Kol
                : OperatorRole.Inviter,
            Status = ApplyStatus.Applying,
            Domain = applyToOperatorInput.Domain,
            DappName = applyToOperatorInput.DappId.ToHex(),
            Descibe = input.Describe,
            ApplyTime = DateTime.UtcNow.ToUtcMilliSeconds()
        };

        var operatorDomainGrain = _clusterClient.GetGrain<IOperatorDomainGrain>(dto.Domain);
        var result =
            await operatorDomainGrain.AddOperatorDomainAsync(dto);

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<OperatorDomainGrainDto, OperatorDomainCreateEto>(result.Data));

        var transactionOutput = await _contractProvider.SendTransactionAsync(input.ChainId, transaction);

        return new ApplyConfirmDto
        {
            TransactionId = transactionOutput.TransactionId
        };
    }

    public async Task<DomainCheckDto> DomainCheckAsync(ApplyCheckInput input)
    {
        var domainCheckDto = new DomainCheckDto();
        if (IsValidDomain(input.Domain) &&
            await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain) != null)
        {
            domainCheckDto.Exists = true;
        }

        return domainCheckDto;
    }


    private bool IsValidDomain(string domain)
    {
        var pattern = @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$";
        return Regex.IsMatch(domain, pattern);
    }
}