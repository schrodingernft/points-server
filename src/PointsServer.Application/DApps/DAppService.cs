using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using Microsoft.Extensions.Options;
using Orleans;
using PointsServer.Common;
using PointsServer.Common.AElfSdk;
using PointsServer.DApps.Dtos;
using PointsServer.DApps.Etos;
using PointsServer.DApps.Provider;
using PointsServer.Grains.Grain.InvitationRelationships;
using PointsServer.Options;
using Volo.Abp;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus.Distributed;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;
using ObjectHelper = PointsServer.Common.ObjectHelper;

namespace PointsServer.DApps;

public class DAppService : IDAppService
{
    private const string InvitationRelationshipsLockPrefix = "PointsServer:Bound:InvitationRelationshipsLock:";
    private readonly IOptionsMonitor<DappOption> _dAppOption;
    private readonly IObjectMapper _objectMapper;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<PointsPlatformSecretOption> _pointsPlatformSecretOption;
    private readonly IOperatorDomainProvider _operatorDomainProvider;

    public DAppService(IOptionsMonitor<DappOption> dAppOption, IObjectMapper objectMapper,
        IAbpDistributedLock distributedLock, IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IContractProvider contractProvider, IOptionsMonitor<PointsPlatformSecretOption> pointsPlatformSecretOption,
        IOperatorDomainProvider operatorDomainProvider)
    {
        _dAppOption = dAppOption;
        _objectMapper = objectMapper;
        _distributedLock = distributedLock;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contractProvider = contractProvider;
        _pointsPlatformSecretOption = pointsPlatformSecretOption;
        _operatorDomainProvider = operatorDomainProvider;
    }

    public async Task<List<DAppDto>> GetDAppListAsync(GetDAppListInput input)
    {
        var filteredDApps = _dAppOption.CurrentValue.DappInfos
            .Where(dApp =>
                (string.IsNullOrEmpty(input.DappName) ||
                 dApp.DappName.Contains(input.DappName, StringComparison.OrdinalIgnoreCase))
                && (!input.Categories.Any() || input.Categories.Contains(dApp.Category)))
            .Select(dApp => _objectMapper.Map<DappInfo, DAppDto>(dApp))
            .ToList();

        return await Task.FromResult(filteredDApps);
    }

    public async Task<List<RoleDto>> GetRolesAsync(bool includePersonal = false)
    {
        var roles = Enum.GetValues(typeof(OperatorRole))
            .Cast<OperatorRole>()
            .Where(role => includePersonal || role != OperatorRole.User)
            .Select(role => new RoleDto { Role = role.ToString() })
            .ToList();
        return await Task.FromResult(roles);
    }

    public async Task<bool> BoundInvitationRelationshipsAsync(BoundInvitationRelationshipsInput input)
    {
        var id = GuidHelper.GenerateId(input.Address, input.Domain, input.DappName);
        await using var handle =
            await _distributedLock.TryAcquireAsync(InvitationRelationshipsLockPrefix + id);

        if (handle == null)
        {
            throw new Exception("Rate limit exceeded");
        }

        var sign = GetSign(input);

        if (sign != input.Signature)
        {
            throw new UserFriendlyException("Signature is invalid");
        }

        var operatorDomain = await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain);
        var dto = _objectMapper.Map<BoundInvitationRelationshipsInput, InvitationRelationshipsGrainDto>(input);
        if (operatorDomain != null)
        {
            dto.OperatorAddress = operatorDomain.Address;
            dto.InviterAddress = operatorDomain.InviterAddress;
        }

        var invitationRelationshipsGrain = _clusterClient.GetGrain<IInvitationRelationshipsGrain>(id);
        var result =
            await invitationRelationshipsGrain.AddInvitationRelationshipsAsync(
                dto);

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<InvitationRelationshipsGrainDto, InvitationRelationshipsCreateEto>(result.Data));
        
        
        // Todo send transaction
        //_contractProvider.CreateTransaction();
        //Todo add points
        return true;
    }

    private string GetSign(object obj)
    {
        var source = ObjectHelper.ConvertObjectToSortedString(obj, "Signature");
        source += _pointsPlatformSecretOption.CurrentValue.DappSecret;
        return HashHelper.ComputeFrom(source).ToHex();
    }
}