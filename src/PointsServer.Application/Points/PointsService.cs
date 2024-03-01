using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using PointsServer.Common;
using PointsServer.Common.AElfSdk;
using PointsServer.DApps.Provider;
using PointsServer.Grains.Grain.Points;
using PointsServer.Options;
using PointsServer.Points.Dtos;
using PointsServer.Points.Etos;
using PointsServer.Points.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using ObjectHelper = PointsServer.Common.ObjectHelper;

namespace PointsServer.Points;

public class PointsService : IPointsService, ISingletonDependency
{
    private const string InvitationRelationshipsLockPrefix = "PointsServer:Bound:InvitationRelationshipsLock:";
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IContractProvider _contractProvider;
    private readonly IOptionsMonitor<PointsPlatformSecretOption> _pointsPlatformSecretOption;
    private readonly IPointsRulesProvider _pointsRulesProvider;
    private readonly IPointsProvider _pointsProvider;
    private readonly ILogger<PointsService> _logger;
    private readonly IInvitationRelationshipsProvider _invitationRelationshipsProvider;
    private readonly IOperatorDomainProvider _operatorDomainProvider;


    public PointsService(IObjectMapper objectMapper, IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, IContractProvider contractProvider,
        IOptionsMonitor<PointsPlatformSecretOption> pointsPlatformSecretOption,
        IPointsProvider pointsProvider,
        IPointsRulesProvider pointsRulesProvider,
        IInvitationRelationshipsProvider invitationRelationshipsProvider,
        IOperatorDomainProvider operatorDomainProvider, ILogger<PointsService> logger)
    {
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _contractProvider = contractProvider;
        _pointsPlatformSecretOption = pointsPlatformSecretOption;
        _pointsRulesProvider = pointsRulesProvider;
        _invitationRelationshipsProvider = invitationRelationshipsProvider;
        _pointsProvider = pointsProvider;
        _operatorDomainProvider = operatorDomainProvider;
        _logger = logger;
    }

    public async Task<PointsRecordResultDto> PointsRecordAsync(PointsRecordInput input)
    {
        var sign = GetSign(input);

        if (sign != input.Signature)
        {
            throw new UserFriendlyException("Signature is invalid");
        }

        var pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, input.RecordAction);

        if (pointsRules == null)
        {
            throw new UserFriendlyException("PointsRules is invalid");
        }

        var batchAddPoints = new List<PointRecordGrainDto>();
        var nowMillisecond = DateTime.Now.Millisecond;
        var personalPointRecord = new PointRecordGrainDto
        {
            Address = input.Address,
            Role = OperatorRole.User,
            Domain = input.Domain,
            DappName = input.DappName,
            RecordAction = input.RecordAction,
            Amount = pointsRules.Amount,
            PointSymbol = pointsRules.Symbol,
            RecordTime = nowMillisecond
        };
        batchAddPoints.Add(personalPointRecord);

        var invitationRelationships =
            await _invitationRelationshipsProvider.GetInvitationRelationshipsAsync(input.Address);
        if (invitationRelationships != null && string.IsNullOrWhiteSpace(invitationRelationships.Address))
        {
            var operatorPointRecord = new PointRecordGrainDto
            {
                Address = invitationRelationships.Address,
                Role = OperatorRole.Kol,
                Domain = input.Domain,
                DappName = input.DappName,
                RecordAction = input.RecordAction,
                Amount = pointsRules.Amount * GetPointsPercentage(pointsRules.PercentageOfTier2Operator),
                PointSymbol = pointsRules.Symbol,
                RecordTime = nowMillisecond
            };
            batchAddPoints.Add(operatorPointRecord);
        }

        if (invitationRelationships != null && string.IsNullOrWhiteSpace(invitationRelationships.InviterAddress))
        {
            var operatorPointRecord = new PointRecordGrainDto
            {
                Address = invitationRelationships.InviterAddress,
                Role = OperatorRole.Inviter,
                Domain = input.Domain,
                DappName = input.DappName,
                RecordAction = input.RecordAction,
                Amount = pointsRules.Amount * GetPointsPercentage(pointsRules.PercentageOfInviter),
                PointSymbol = pointsRules.Symbol,
                RecordTime = nowMillisecond
            };
            batchAddPoints.Add(operatorPointRecord);
        }

        await BatchAddPointsAsync(batchAddPoints);

        return new PointsRecordResultDto();
    }

    public async Task<PagedResultDto<RankingListDto>> GetRankingListAsync(GetRankingListInput input)
    {
        _logger.LogInformation("GetRankingListAsync, req:{req}", JsonConvert.SerializeObject(input));
        var pointsList = await
            _pointsProvider.GetOperatorPointsSumIndexListAsync(
                _objectMapper.Map<GetRankingListInput, GetOperatorPointsSumIndexListInput>(input));


        var resp = new PagedResultDto<RankingListDto>();
        if (pointsList.TotalCount == 0)
        {
            return resp;
        }

        resp.TotalCount = pointsList.TotalCount;
        var items = new List<RankingListDto>();

        foreach (var index in pointsList.IndexList)
        {
            var dto = _objectMapper.Map<OperatorPointSumIndex, RankingListDto>(index);
            dto.FollowersNumber = await _invitationRelationshipsProvider.CountDomainFollowersAsync(index.Domain);
            items.Add(dto);
        }

        resp.Items = items;

        _logger.LogInformation("GetRankingListAsync, resp:{resp}", JsonConvert.SerializeObject(resp));
        return resp;
    }

    public async Task<RankingDetailDto> GetRankingDetailAsync(GetRankingDetailInput input)
    {
        _logger.LogInformation("GetRankingDetailAsync, req:{req}", JsonConvert.SerializeObject(input));
        var queryInput = _objectMapper.Map<GetRankingDetailInput, GetOperatorPointsActionSumInput>(input);
        queryInput.Role = OperatorRole.Kol;
        var actionRecordPoints = await _pointsProvider.GetOperatorPointsActionSumAsync(queryInput);

        var resp = new RankingDetailDto();
        if (actionRecordPoints.TotalCount == 0)
        {
            return resp;
        }

        var actionPointList =
            _objectMapper.Map<List<OperatorPointActionSumIndex>, List<ActionPoints>>(actionRecordPoints.IndexList);
        resp.PointDetails = actionPointList;

        var domainInfo = await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain);
        if (domainInfo != null)
        {
            resp.Describe = domainInfo.Descibe;
            resp.Icon = domainInfo.Icon;
        }

        _logger.LogInformation("GetRankingDetailAsync, resp:{req}", JsonConvert.SerializeObject(input));
        return resp;
    }

    public async Task<GetPointsEarnedListDto> GetPointsEarnedListAsync(GetPointsEarnedListInput input)
    {
        _logger.LogInformation("GetPointsEarnedListAsync, req:{req}", JsonConvert.SerializeObject(input));
        var queryInput =
            _objectMapper.Map<GetPointsEarnedListInput, GetOperatorPointsSumIndexListByAddressInput>(input);
        var pointsList = await _pointsProvider.GetOperatorPointsSumIndexListByAddressAsync(queryInput);

        var resp = new GetPointsEarnedListDto();
        if (pointsList.TotalCount == 0)
        {
            return resp;
        }

        resp.TotalCount = pointsList.TotalCount;
        resp.Items = _objectMapper.Map<List<OperatorPointSumIndex>, List<PointsEarnedListDto>>(pointsList.IndexList);


        decimal totalEarnings = 0;
        long remain = 0;
        const int maxResultCount = 20;
        queryInput.SkipCount = 0;
        do
        {
            var ret = await _pointsProvider.GetOperatorPointsSumIndexListByAddressAsync(queryInput);

            //ret.IndexList.ToList().ForEach(index => { totalEarnings += index.Amount; });

            queryInput.SkipCount += maxResultCount;
            remain = ret.TotalCount - queryInput.SkipCount;
        } while (remain > 0);

        resp.TotalEarned = totalEarnings;

        _logger.LogInformation("GetPointsEarnedListAsync, resp:{resp}", JsonConvert.SerializeObject(resp));
        return resp;
    }

    public async Task<PointsEarnedDetailDto> GetPointsEarnedDetailAsync(GetPointsEarnedDetailInput input)
    {
        _logger.LogInformation("GetPointsEarnedDetailAsync, req:{req}", JsonConvert.SerializeObject(input));
        var queryInput = _objectMapper.Map<GetPointsEarnedDetailInput, GetOperatorPointsActionSumInput>(input);
        var actionRecordPoints = await _pointsProvider.GetOperatorPointsActionSumAsync(queryInput);

        var resp = new PointsEarnedDetailDto();
        if (actionRecordPoints.TotalCount == 0)
        {
            return resp;
        }

        var actionPointList =
            _objectMapper.Map<List<OperatorPointActionSumIndex>, List<ActionPoints>>(actionRecordPoints.IndexList);
        resp.PointDetails = actionPointList;

        var domainInfo = await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain);
        if (domainInfo != null)
        {
            resp.Describe = domainInfo.Descibe;
            resp.Icon = domainInfo.Icon;
        }

        _logger.LogInformation("GetPointsEarnedDetailAsync, resp:{req}", JsonConvert.SerializeObject(input));
        return resp;
    }

    private async Task BatchAddPointsAsync(List<PointRecordGrainDto> pointRecords)
    {
        await Task.WhenAll(pointRecords.Select(AddPointsAsync));
    }

    private async Task AddPointsAsync(PointRecordGrainDto pointRecordGrain)
    {
        var id = GuidHelper.GenerateId(pointRecordGrain.Address, pointRecordGrain.Domain, pointRecordGrain.DappName,
            pointRecordGrain.RecordAction);

        var operatorPointRecordDetailGrain = _clusterClient.GetGrain<IOperatorPointRecordDetailGrain>(id);

        var result =
            await operatorPointRecordDetailGrain.PointsRecordAsync(pointRecordGrain);

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<PointRecordGrainDto, PointRecordEto>(result.Data));
    }

    private decimal GetPointsPercentage(string percentageRule)
    {
        var parts = percentageRule.Split(':');
        if (parts.Length != 2)
        {
            throw new ArgumentException("The percentageRule format is incorrect.");
        }


        var numerator = decimal.Parse(parts[0]);
        var denominator = decimal.Parse(parts[1]);


        if (denominator == 0)
        {
            throw new DivideByZeroException("The denominator cannot be zero.");
        }

        return numerator / denominator;
    }

    private string GetSign(object obj)
    {
        var source = ObjectHelper.ConvertObjectToSortedString(obj, "Signature");
        source += _pointsPlatformSecretOption.CurrentValue.DappSecret;
        return HashHelper.ComputeFrom(source).ToHex();
    }
}