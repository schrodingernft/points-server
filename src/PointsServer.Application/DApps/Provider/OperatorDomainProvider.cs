using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.Operator;
using Volo.Abp.DependencyInjection;

namespace PointsServer.DApps.Provider;

public interface IOperatorDomainProvider
{
    Task<OperatorDomainInfoIndex> GetOperatorDomainIndexAsync(string domain);
}

public class OperatorDomainProvider : IOperatorDomainProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorDomainInfoIndex, string> _repository;

    public OperatorDomainProvider(INESTRepository<OperatorDomainInfoIndex, string> repository)
    {
        _repository = repository;
    }

    public async Task<OperatorDomainInfoIndex> GetOperatorDomainIndexAsync(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorDomainInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Domain)
            .Terms(domain)));

        QueryContainer Filter(QueryContainerDescriptor<OperatorDomainInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        
        return await _repository.GetAsync(Filter);
    }
}