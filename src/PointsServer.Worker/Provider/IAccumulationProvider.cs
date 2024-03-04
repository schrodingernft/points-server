using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using PointsServer.Points;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface IAccumulationProvider
{
    Task<List<OperatorPointSumIndex>> GetOperatorPointSumListAsync(int skipCount, int maxResultCount);
    Task UpdateOperatorPointSumAsync(List<OperatorPointSumIndex> operatorPointSumList);
}

public class AccumulationProvider : IAccumulationProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorPointSumIndex, string> _operatorPointSumRepository;

    public AccumulationProvider(INESTRepository<OperatorPointSumIndex, string> operatorPointSumRepository)
    {
        _operatorPointSumRepository = operatorPointSumRepository;
    }

    public async Task<List<OperatorPointSumIndex>> GetOperatorPointSumListAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _operatorPointSumRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }
    
    public async Task UpdateOperatorPointSumAsync(List<OperatorPointSumIndex> operatorPointSumList)
    {
        await _operatorPointSumRepository.BulkAddOrUpdateAsync(operatorPointSumList);
    }
}