using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using PointsServer.Points;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface ICalculationProvider
{
    Task<List<OperatorPointsSumIndex>> GetOperatorPointSumListAsync(int skipCount, int maxResultCount);
    Task UpdateOperatorPointSumAsync(List<OperatorPointsSumIndex> operatorPointSumList);
}

public class CalculationProvider : ICalculationProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorPointsSumIndex, string> _operatorPointSumRepository;

    public CalculationProvider(INESTRepository<OperatorPointsSumIndex, string> operatorPointSumRepository)
    {
        _operatorPointSumRepository = operatorPointSumRepository;
    }

    public async Task<List<OperatorPointsSumIndex>> GetOperatorPointSumListAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) =
            await _operatorPointSumRepository.GetListAsync(skip: skipCount, limit: maxResultCount);

        return data;
    }
    
    public async Task UpdateOperatorPointSumAsync(List<OperatorPointsSumIndex> operatorPointSumList)
    {
        await _operatorPointSumRepository.BulkAddOrUpdateAsync(operatorPointSumList);
    }
}