using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using PointsServer.Operator;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface IAccumulationProvider
{
    Task<List<OperatorDomainIndex>> GetOperatorDomainListAsync(int skipCount, int maxResultCount);
}

public class AccumulationProvider : IAccumulationProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorDomainIndex, string> _operatorDomainRepository;

    public AccumulationProvider(INESTRepository<OperatorDomainIndex, string> operatorDomainRepository)
    {
        _operatorDomainRepository = operatorDomainRepository;
    }

    public async Task<List<OperatorDomainIndex>> GetOperatorDomainListAsync(int skipCount, int maxResultCount)
    {
        var (totalCount, data) = await _operatorDomainRepository.GetListAsync(skip: skipCount, limit: maxResultCount);
        return data;
    }
}