using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.Points;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface IPointsSettlementProvider
{
    Task<List<OperatorPointRecordDetailIndex>> GetPointRecordListAsync(long startTime, long endTime);
}

public class PointsSettlementProvider : IPointsSettlementProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorPointRecordDetailIndex, string> _repository;

    public PointsSettlementProvider(INESTRepository<OperatorPointRecordDetailIndex, string> repository)
    {
        _repository = repository;
    }


    public async Task<List<OperatorPointRecordDetailIndex>> GetPointRecordListAsync(long startTime, long endTime)
    {
        if (startTime > endTime)
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorPointRecordDetailIndex>, QueryContainer>>();
        mustQuery.Add(q => q.LongRange(i => i.Field(f => f.RecordTime).GreaterThanOrEquals(startTime)));
        mustQuery.Add(q => q.LongRange(i => i.Field(f => f.RecordTime).LessThan(endTime)));

        QueryContainer Filter(QueryContainerDescriptor<OperatorPointRecordDetailIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _repository.GetListAsync(Filter);
        return result.Item2;
    }
}