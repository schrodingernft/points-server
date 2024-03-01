using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Orleans;
using Points.Contracts.Point;
using PointsServer.Common;
using PointsServer.Common.AElfSdk;
using PointsServer.Grains.Grain.Points;
using PointsServer.Grains.State.Worker;
using PointsServer.Points;
using PointsServer.Points.Etos;
using PointsServer.Worker.Provider;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace PointsServer.Worker.Services;

public interface IPointsSettlementService
{
    Task PointsSettlementAsync();
}

public class PointsSettlementService : IPointsSettlementService, ISingletonDependency
{
    private readonly IPointsSettlementProvider _pointsSettlementProvider;
    private readonly ILogger<PointsSettlementService> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly ILatestExecuteTimeProvider _latestExecuteTimeProvider;

    public PointsSettlementService(IPointsSettlementProvider pointsSettlementProvider,
        ILogger<PointsSettlementService> logger, IContractProvider contractProvider, IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, IObjectMapper objectMapper,
        ILatestExecuteTimeProvider latestExecuteTimeProvider)
    {
        _pointsSettlementProvider = pointsSettlementProvider;
        _logger = logger;
        _contractProvider = contractProvider;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _latestExecuteTimeProvider = latestExecuteTimeProvider;
    }

    public async Task PointsSettlementAsync()
    {
        var nowMillisecond = DateTime.Now.Millisecond;
        var type = CommonConstant.PointsSettlementWorker;
        var latestExecuteTime =
            await _latestExecuteTimeProvider.GetLatestExecuteTimeAsync(type);
        var pointRecordList =
            await _pointsSettlementProvider.GetPointRecordListAsync(latestExecuteTime, nowMillisecond);

        await PointsSettlementByRankingAsync(pointRecordList);

        await PointsSettlementByEarnedDetailAsync(pointRecordList);

        await SendPointsSettlementTransactionAsync(pointRecordList);

        await _latestExecuteTimeProvider.UpdateLatestExecuteTimeAsync(new WorkerOptionState
        {
            Type = type,
            LatestExecuteTime = nowMillisecond
        });
    }

    private async Task PointsSettlementByEarnedDetailAsync(List<OperatorPointRecordDetailIndex> list)
    {
        var nowMillisecond = DateTime.Now.Millisecond;
        var pointActionSumRecords = list
            .GroupBy(record => new { record.Address, record.DappName, record.Domain, record.Role, record.RecordAction })
            .Select(group => new OperatorPointActionSumGrainDto
            {
                Domain = group.Key.Domain,
                DappName = group.Key.DappName,
                Address = group.Key.Address,
                Role = group.Key.Role,
                RecordAction = group.Key.RecordAction,
                Amount = group.Sum(record => record.Amount),
                UpdateTime = nowMillisecond
            })
            .ToList();

        foreach (var pointActionSumRecord in pointActionSumRecords)
        {
            var id = GuidHelper.GenerateId(pointActionSumRecord.Address, pointActionSumRecord.DappName,
                pointActionSumRecord.Domain,
                pointActionSumRecord.Role.ToString(), pointActionSumRecord.RecordAction);

            var operatorPointActionSumGrain = _clusterClient.GetGrain<IOperatorPointActionSumGrain>(id);

            var result =
                await operatorPointActionSumGrain.UpdatePointsActionSumAsync(pointActionSumRecord);

            if (!result.Success)
            {
                throw new UserFriendlyException(result.Message);
            }

            await _distributedEventBus.PublishAsync(
                _objectMapper.Map<OperatorPointActionSumGrainDto, OperatorPointSumEto>(result.Data));
        }
    }

    private async Task PointsSettlementByRankingAsync(List<OperatorPointRecordDetailIndex> list)
    {
        var nowMillisecond = DateTime.Now.Millisecond;
        var pointSumRecords = list
            .GroupBy(record => new { record.Domain, record.DappName, record.Address, record.Role })
            .Select(group => new OperatorPointSumGrainDto
            {
                Domain = group.Key.Domain,
                DappName = group.Key.DappName,
                Address = group.Key.Address,
                Role = group.Key.Role,
                Amount = group.Sum(record => record.Amount),
                UpdateTime = nowMillisecond
            })
            .ToList();
        foreach (var pointSumRecord in pointSumRecords)
        {
            var id = GuidHelper.GenerateId(pointSumRecord.Domain, pointSumRecord.DappName, pointSumRecord.Address,
                pointSumRecord.Role.ToString());

            var operatorPointSumGrain = _clusterClient.GetGrain<IOperatorPointSumGrain>(id);

            var result =
                await operatorPointSumGrain.UpdatePointsSumAsync(pointSumRecord);

            if (!result.Success)
            {
                throw new UserFriendlyException(result.Message);
            }

            await _distributedEventBus.PublishAsync(
                _objectMapper.Map<OperatorPointSumGrainDto, OperatorPointSumEto>(result.Data));
        }
    }

    private async Task SendPointsSettlementTransactionAsync(List<OperatorPointRecordDetailIndex> list)
    {
        var pointsRecord = list
            .GroupBy(record => new { record.Address, record.DappName, record.PointSymbol })
            .Select(group => new PointsRecord
            {
                PointerAddress = Address.FromBase58(group.Key.Address),
                PointsName = group.Key.PointSymbol,
                Amout = Convert.ToInt64(Math.Round(group.Sum(record => record.Amount) *
                                                   Convert.ToDecimal(Math.Pow(10, 8))))
            })
            .ToList();
        var pointsSettlementInput = new PointsSettlementInput
        {
            PointsRecords = { pointsRecord }
        };

        await _contractProvider.CreateTransaction("", "", "", ContractConstant.PointsSettlement,
            pointsSettlementInput);
    }
}