using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.Points;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface IScoreDataNotificationProvider
{
    Task<List<OperatorPointSumIndex>> GetOperatorPointAsync(long startTime, long endTime);
}

public class ScoreDataNotificationProvider : IScoreDataNotificationProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorPointSumIndex, string> _operatorPointSumRepository;

    public ScoreDataNotificationProvider(INESTRepository<OperatorPointSumIndex, string> operatorPointSumRepository)
    {
        _operatorPointSumRepository = operatorPointSumRepository;
    }

    public async Task<List<OperatorPointSumIndex>> GetOperatorPointAsync(long startTime, long endTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorPointSumIndex>, QueryContainer>>() { };
        // mustQuery.Add(q => q.Term(i => i.Field(f => f.IdentifierHash).Value(identifierHash)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.UpdateTime).GreaterThanOrEquals(startTime)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.UpdateTime).LessThanOrEquals(endTime)));


        QueryContainer Filter(QueryContainerDescriptor<OperatorPointSumIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, data) = await _operatorPointSumRepository.GetListAsync(Filter);

        return data;
    }
}