using System;
using System.Collections.Generic;
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


    public PointsSumService(IPointsIndexerProvider pointsIndexerProvider, LatestExecuteTimeProvider latestExecuteTimeProvider,
        IObjectMapper objectMapper, INESTRepository<OperatorPointSumIndex, string> repository)
    {
        _pointsIndexerProvider = pointsIndexerProvider;
        _latestExecuteTimeProvider = latestExecuteTimeProvider;
        _objectMapper = objectMapper;
        _repository = repository;
    }

    public async Task RecordPointsSumAsync()
    {
        var nowMillisecond = DateTime.Now.Millisecond;
        var type = CommonConstant.PointsSumWorker;
        var latestExecuteTime =
            await _latestExecuteTimeProvider.GetLatestExecuteTimeAsync(type);

        var pointsSumList =
            await _pointsIndexerProvider.GetPointsSumListAsync(latestExecuteTime, nowMillisecond);
        var pointsSumIndexList = new List<OperatorPointSumIndex>();

        foreach (var pointsSumDto in pointsSumList)
        {
            var operatorPointSumIndex = _objectMapper.Map<PointsSumDto, OperatorPointSumIndex>(pointsSumDto);
            if (operatorPointSumIndex.Role == OperatorRole.User)
            {
                //get Kol and Inviter address
            }

            if (operatorPointSumIndex.Role == OperatorRole.Kol)
            {
                //get Inviter address
            }

            pointsSumIndexList.Add(operatorPointSumIndex);
        }
        

        await _latestExecuteTimeProvider.UpdateLatestExecuteTimeAsync(new WorkerOptionState
        {
            Type = type,
            LatestExecuteTime = nowMillisecond
        });

        throw new NotImplementedException();
    }
}