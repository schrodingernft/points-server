using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using PointsServer.Common;
using PointsServer.DApps;
using PointsServer.DApps.Dtos;
using PointsServer.Grains.State.Worker;
using PointsServer.Operator;
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
    private readonly INESTRepository<OperatorPointsRankSumIndex, string> _repository;
    private readonly IPointsSumProvider _pointsSumProvider;
    private readonly ILogger<PointsSumService> _logger;
    private readonly IDAppService _dAppService;

    public PointsSumService(IPointsIndexerProvider pointsIndexerProvider,
        LatestExecuteTimeProvider latestExecuteTimeProvider,
        IObjectMapper objectMapper, INESTRepository<OperatorPointsRankSumIndex, string> repository,
        IPointsSumProvider pointsSumProvider, IPointsProvider pointsProvider, ILogger<PointsSumService> logger, 
        IDAppService dAppService)
    {
        _pointsIndexerProvider = pointsIndexerProvider;
        _latestExecuteTimeProvider = latestExecuteTimeProvider;
        _objectMapper = objectMapper;
        _repository = repository;
        _pointsSumProvider = pointsSumProvider;
        _pointsProvider = pointsProvider;
        _logger = logger;
        _dAppService = dAppService;
    }

    public async Task RecordPointsSumAsync()
    {
        var now = DateTime.UtcNow;
        var type = CommonConstant.PointsSumWorker;
        var latestExecuteTime =
            await _latestExecuteTimeProvider.GetLatestExecuteTimeAsync(type);

        var pointsSumList =
            await _pointsIndexerProvider.GetPointsSumListAsync(latestExecuteTime, now);
        _logger.LogInformation("RecordPointsSumAsync GetIndexerPointsSumList, latestTime: {latestExecuteTime}," +
                               " now: {now}, count: {count}", latestExecuteTime, now, pointsSumList.Count);

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
        
        try
        {
            var dappDomainDic = _dAppService.GetDappDomainDic();

            var pointsSumIndexList = await ConvertDtoToIndexAsync(pointsSumList, domainDic, dappDomainDic);

            if (pointsSumIndexList.IsNullOrEmpty())
            {
                _logger.LogInformation("RecordPointsSumAsync BulkAddOrUpdateAsync is null");
                return;
            }

            await _repository.BulkAddOrUpdateAsync(pointsSumIndexList);
            _logger.LogInformation("RecordPointsSumAsync BulkAddOrUpdateAsync, count: {count}", pointsSumIndexList.Count);

            var latestExecuteMaxTime = pointsSumIndexList
                .Select(point => point.UpdateTime).Max() - 1000;
            await _latestExecuteTimeProvider.UpdateLatestExecuteTimeAsync(new WorkerOptionState
            {
                Type = type,
                LatestExecuteTime = latestExecuteMaxTime
            });
            _logger.LogInformation("RecordPointsSumAsync end. LatestExecuteMaxTime: {time}", latestExecuteMaxTime);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "RecordPointsSumAsync fail, time: {time}", now);
        }
    }

    private async Task<List<OperatorPointsRankSumIndex>> ConvertDtoToIndexAsync(List<PointsSumDto> pointsSumList,
        IReadOnlyDictionary<string, OperatorDomainInfoIndex> domainDic, Dictionary<string, DAppDto> dappDomainDic)
    {
        var pointsSumIndexList = new List<OperatorPointsRankSumIndex>();

        foreach (var pointsSumDto in pointsSumList)
        {
            var operatorPointSumIndex = _objectMapper.Map<PointsSumDto, OperatorPointsRankSumIndex>(pointsSumDto);
            var domain = operatorPointSumIndex.Domain;
            var relationshipFlag = domainDic.TryGetValue(domain, out var operatorDomain);
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
                    "RecordPointsSumAsync: local Es not find,to indexer find begin, domain: {domain}", domain);
                if (dappDomainDic.TryGetValue(domain, out var dappDto))
                {
                    operatorPointSumIndex.DappName = dappDto.DappId;
                }
                else
                {
                    var operatorDomainDto = await _pointsProvider.GetOperatorDomainInfoAsync(
                        new GetOperatorDomainInfoInput()
                        {
                            Domain = domain
                        });
                    if (operatorDomainDto != null)
                    {
                        operatorPointSumIndex.DappName = operatorDomainDto.DappId;
                        _logger.LogInformation(
                            "RecordPointsSumAsync: local Es not find,to indexer find end, domain: {domain}",
                            operatorDomainDto.Domain);
                    }
                }
            }

            pointsSumIndexList.Add(operatorPointSumIndex);
        }

        return pointsSumIndexList;
    }
}