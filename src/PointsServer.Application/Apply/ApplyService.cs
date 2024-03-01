using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Options;
using Orleans;
using Points.Contracts.Point;
using PointsServer.Apply.Dtos;
using PointsServer.Apply.Etos;
using PointsServer.Common;
using PointsServer.Common.AElfSdk;
using PointsServer.Common.Dto;
using PointsServer.Common.GraphQL;
using PointsServer.DApps.Provider;
using PointsServer.Grains.Grain.Operator;
using PointsServer.Options;
using PointsServer.Users.Provider;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace PointsServer.Apply;

public class ApplyService : PointsPlatformAppService, IApplyService
{
    private readonly IOperatorDomainProvider _operatorDomainProvider;
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly IOptionsMonitor<GraphQLOption> _graphQlOption;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IContractProvider _contractProvider;

    public ApplyService(IOperatorDomainProvider operatorDomainProvider, IGraphQlHelper graphQlHelper,
        IOptionsMonitor<GraphQLOption> graphQlOption, IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, IObjectMapper objectMapper,
        IUserInformationProvider userInformationProvider, IContractProvider contractProvider)
    {
        _operatorDomainProvider = operatorDomainProvider;
        _graphQlHelper = graphQlHelper;
        _graphQlOption = graphQlOption;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _userInformationProvider = userInformationProvider;
        _contractProvider = contractProvider;
    }

    public async Task<ApplyCheckResultDto> ApplyCheckAsync(ApplyCheckInput input)
    {
        var result = new ApplyCheckResultDto();
        //check domain format & uniqueness
        if (!IsValidDomain(input.Domain))
        {
            result.DomainCheck = "invalid domain format";
        }

        if (await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain) != null)
        {
            result.DomainCheck = "this domain already existed";
        }

        //check address invalid
        var caHolderInfos = await GetCaHolderInfo(_graphQlOption.CurrentValue.PortkeyV2Url, input.Address);

        if (caHolderInfos == null)
        {
            result.DomainCheck = "invalid address";
        }

        return result;
    }

    public async Task<ApplyConfirmDto> ApplyConfirmAsync(ApplyConfirmInput input)
    {
        var transaction =
            Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(input.RawTransaction));

        if (!VerifyHelper.VerifySignature(transaction, input.PublicKey))
            throw new UserFriendlyException("RawTransaction validation failed");

        var applyToOperatorInput = ApplyToOperatorInput.Parser.ParseFrom(transaction.Params);

        var userInfo = await _userInformationProvider.GetUserById(CurrentUser.GetId());

        var dto = new OperatorDomainGrainDto()
        {
            Address = applyToOperatorInput.Invitee.ToBase58(),
            InviterAddress = userInfo.CaAddressMain,
            Role = applyToOperatorInput.Invitee.ToBase58() == userInfo.CaAddressMain
                ? OperatorRole.Tier2Operator
                : OperatorRole.Inviter,
            Status = ApplyStatus.Applying,
            Domain = applyToOperatorInput.Domain,
            DappName = applyToOperatorInput.Service,
            Descibe = input.Describe,
            ApplyTime = DateTime.Now.Millisecond
        };

        var id = GuidHelper.GenerateId(dto.Address, dto.Domain, dto.DappName);

        var operatorDomainGrain = _clusterClient.GetGrain<IOperatorDomainGrain>(id);
        var result =
            await operatorDomainGrain.AddOperatorDomainAsync(dto);

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<OperatorDomainGrainDto, OperatorDomainCreateEto>(result.Data));

        var transactionOutput = await _contractProvider.SendTransactionAsync("", transaction);

        return new ApplyConfirmDto
        {
            TransactionId = transactionOutput.TransactionId
        };
    }


    private async Task<IndexerCAHolderInfos> GetCaHolderInfo(string url, string managerAddress, string? chainId = null)
    {
        using var graphQlClient = new GraphQLHttpClient(url, new NewtonsoftJsonSerializer());

        // It should just one item
        var graphQlRequest = new GraphQLRequest
        {
            Query = @"query(
                    $manager:String
                    $skipCount:Int!,
                    $maxResultCount:Int!
                ) {
                    caHolderManagerInfo(dto: {
                        manager:$manager,
                        skipCount:$skipCount,
                        maxResultCount:$maxResultCount
                    }){
                        chainId,
                        caHash,
                        caAddress,
                        managerInfos{ address }
                    }
                }",
            Variables = new
            {
                chainId = chainId, manager = managerAddress, skipCount = 0, maxResultCount = 10
            }
        };

        var graphQlResponse = await graphQlClient.SendQueryAsync<IndexerCAHolderInfos>(graphQlRequest);
        return graphQlResponse.Data;
    }

    private bool IsValidDomain(string domain)
    {
        var pattern = @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$";
        return Regex.IsMatch(domain, pattern);
    }
}