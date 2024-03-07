using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using PointsServer.Common;
using PointsServer.Grains.State.Worker;
using PointsServer.Points;
using PointsServer.Points.Dtos;
using PointsServer.Points.Provider;
using PointsServer.Worker.Provider;
using PointsServer.Worker.Provider.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace PointsServer.Worker.Services;

public interface IPointsSumService
{
    Task RecordPointsSumAsync();
}

public class PointsSumService : IPointsSumService, ISingletonDependency
{
    private readonly IPointsIndexerProvider _pointsIndexerProvider;
    private readonly IPointsProvider _pointsProvider;

    private readonly LatestExecuteTimeProvider _latestExecuteTimeProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<OperatorPointsSumIndex, string> _repository;
    private readonly IPointsSumProvider _pointsSumProvider;
    private readonly ILogger<PointsSumService> _logger;

    public PointsSumService(IPointsIndexerProvider pointsIndexerProvider,
        LatestExecuteTimeProvider latestExecuteTimeProvider,
        IObjectMapper objectMapper, INESTRepository<OperatorPointsSumIndex, string> repository,
        IPointsSumProvider pointsSumProvider, IPointsProvider pointsProvider, ILogger<PointsSumService> logger)
    {
        _pointsIndexerProvider = pointsIndexerProvider;
        _latestExecuteTimeProvider = latestExecuteTimeProvider;
        _objectMapper = objectMapper;
        _repository = repository;
        _pointsSumProvider = pointsSumProvider;
        _pointsProvider = pointsProvider;
        _logger = logger;
    }

    public async Task RecordPointsSumAsync()
    {
        var now = DateTime.UtcNow;
        var type = CommonConstant.PointsSumWorker;
        var latestExecuteTime =
            await _latestExecuteTimeProvider.GetLatestExecuteTimeAsync(type);

        var pointsSumList =
            await _pointsIndexerProvider.GetPointsSumListAsync(latestExecuteTime, now);
        var pointsSumIndexList = new List<OperatorPointsSumIndex>();

        var userAddresses = pointsSumList
            .Where(pointSum => pointSum.Role == OperatorRole.User)
            .Select(pointSum => pointSum.Address)
            .ToList();

        var domainUserRelationShipList = await _pointsIndexerProvider.GetDomainUserRelationshipsAsync(
            new DomainUserRelationShipInput()
            {
                Addresses = userAddresses
            });

        var userRoleDomains = domainUserRelationShipList.Select(relationShip => relationShip.Domain).ToList();

        var kolRoleDomains = pointsSumList
            .Where(pointSum => pointSum.Role == OperatorRole.Kol)
            .Select(pointSum => pointSum.Domain)
            .ToList();
        userRoleDomains.AddRange(kolRoleDomains);

        var domainDic =
            await _pointsSumProvider.GetKolInviterRelationShipByDomainsAsync(userRoleDomains.Distinct().ToList());


        foreach (var pointsSumDto in pointsSumList)
        {
            var operatorPointSumIndex = _objectMapper.Map<PointsSumDto, OperatorPointsSumIndex>(pointsSumDto);
            var relationshipFlag = domainDic.TryGetValue(operatorPointSumIndex.Domain, out var operatorDomain);
            switch (operatorPointSumIndex.Role)
            {
                case OperatorRole.User when relationshipFlag:
                    //set Kol and Inviter address
                    operatorPointSumIndex.KolAddress = operatorDomain.Address;
                    if (operatorDomain.Role == OperatorRole.Inviter)
                    {
                        operatorPointSumIndex.InviterAddress = operatorDomain.InviterAddress;
                    }

                    break;
                case OperatorRole.Kol when relationshipFlag:
                    //set Inviter address
                    if (operatorDomain.Role == OperatorRole.Inviter)
                    {
                        operatorPointSumIndex.InviterAddress = operatorDomain.InviterAddress;
                    }

                    break;
            }
            
            if (relationshipFlag)
            {
                operatorPointSumIndex.DappName = operatorDomain.DappName;
            }
            
            //query indexer
            if (!relationshipFlag)
            {
                _logger.LogInformation(
                    "RecordPointsSumAsync: local Es not find,to indexer find begin, domain: {domain}", operatorPointSumIndex.Domain);å
                var operatorDomainDto = await _pointsProvider.GetOperatorDomainInfoAsync(new GetOperatorDomainInfoInput()
                {
                    Domain = operatorPointSumIndex.Domain
                });
                if (operatorDomainDto != null)
                {
                    operatorPointSumIndex.DappName = operatorDomainDto.DappId;
                }
                _logger.LogInformation(
                    "RecordPointsSumAsync: local Es not find,to indexer find end, domain: {domain}", operatorDomainDto.Domain);
            }

            pointsSumIndexList.Add(operatorPointSumIndex);
        }

        if (!pointsSumIndexList.IsNullOrEmpty())
        {
            _logger.LogInformation(
                "BulkAddOrUpdateAsync, count: {count}", pointsSumIndexList.Count);
            await _repository.BulkAddOrUpdateAsync(pointsSumIndexList);
        }

        var latestExecuteMaxTime = pointsSumIndexList.Select(point => point.UpdateTime).Max();
        _logger.LogInformation(
            "LatestExecuteMaxTime, time: {time}", latestExecuteMaxTime);
        await _latestExecuteTimeProvider.UpdateLatestExecuteTimeAsync(new WorkerOptionState
        {
            Type = type,
            LatestExecuteTime = latestExecuteMaxTime
        });
    }
}