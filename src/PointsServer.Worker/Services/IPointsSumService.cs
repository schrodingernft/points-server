using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using PointsServer.Common;
using PointsServer.Grains.State.Worker;
using PointsServer.Points;
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
    private readonly LatestExecuteTimeProvider _latestExecuteTimeProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<OperatorPointSumIndex, string> _repository;
    private readonly IPointsSumProvider _pointsSumProvider;


    public PointsSumService(IPointsIndexerProvider pointsIndexerProvider,
        LatestExecuteTimeProvider latestExecuteTimeProvider,
        IObjectMapper objectMapper, INESTRepository<OperatorPointSumIndex, string> repository,
        IPointsSumProvider pointsSumProvider)
    {
        _pointsIndexerProvider = pointsIndexerProvider;
        _latestExecuteTimeProvider = latestExecuteTimeProvider;
        _objectMapper = objectMapper;
        _repository = repository;
        _pointsSumProvider = pointsSumProvider;
    }

    public async Task RecordPointsSumAsync()
    {
        var now = DateTime.UtcNow;
        var type = CommonConstant.PointsSumWorker;
        var latestExecuteTime =
            await _latestExecuteTimeProvider.GetLatestExecuteTimeAsync(type);

        var pointsSumList =
            await _pointsIndexerProvider.GetPointsSumListAsync(latestExecuteTime, now);
        var pointsSumIndexList = new List<OperatorPointSumIndex>();

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
            var operatorPointSumIndex = _objectMapper.Map<PointsSumDto, OperatorPointSumIndex>(pointsSumDto);
            var relationshipFlag = domainDic.TryGetValue(operatorPointSumIndex.Domain, out var operatorDomain);
            switch (operatorPointSumIndex.Role)
            {
                case OperatorRole.User when relationshipFlag:
                    //set Kol and Inviter address
                    operatorPointSumIndex.KolAddress = operatorDomain.Address;
                    operatorPointSumIndex.InviterAddress = operatorDomain.InviterAddress;
                    break;
                case OperatorRole.Kol when relationshipFlag:
                    //set Inviter address
                    operatorPointSumIndex.InviterAddress = operatorDomain.InviterAddress;
                    break;
            }

            pointsSumIndexList.Add(operatorPointSumIndex);
        }

        await _repository.BulkAddOrUpdateAsync(pointsSumIndexList);

        await _latestExecuteTimeProvider.UpdateLatestExecuteTimeAsync(new WorkerOptionState
        {
            Type = type,
            LatestExecuteTime = now.ToUtcMilliSeconds()
        });
    }
}