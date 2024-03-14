using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using PointsServer.Points;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface ICalculationProvider
{
    Task<List<OperatorPointsRankSumIndex>> GetOperatorPointSumListAsync(int skipCount, int maxResultCount);
    Task UpdateOperatorPointSumAsync(List<OperatorPointsRankSumIndex> operatorPointSumList);
}

public class CalculationProvider : ICalculationProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorPointsRankSumIndex, string> _operatorPointSumRepository;

    public CalculationProvider(INESTRepository<OperatorPointsRankSumIndex, string> operatorPointSumRepository)
    {
        _operatorPointSumRepository = operatorPointSumRepository;
    }

    public async Task<List<OperatorPointsRankSumIndex>> GetOperatorPointSumListAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _operatorPointSumRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }
    
    public async Task UpdateOperatorPointSumAsync(List<OperatorPointsRankSumIndex> operatorPointSumList)
    {
        await _operatorPointSumRepository.BulkAddOrUpdateAsync(operatorPointSumList);
    }
}