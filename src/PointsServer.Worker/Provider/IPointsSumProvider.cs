using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.Operator;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface IPointsSumProvider
{
    Task<Dictionary<string, OperatorDomainIndex>> GetKolInviterRelationShipByDomainsAsync(List<string> domains);
}

public class PointsSumProvider : IPointsSumProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorDomainIndex, string> _repository;

    public PointsSumProvider(INESTRepository<OperatorDomainIndex, string> repository)
    {
        _repository = repository;
    }

    public async Task<Dictionary<string, OperatorDomainIndex>> GetKolInviterRelationShipByDomainsAsync(List<string> domains)
    {
        if (domains.IsNullOrEmpty())
        {
            return new Dictionary<string, OperatorDomainIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorDomainIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Domain).Terms(domains)));

        QueryContainer Filter(QueryContainerDescriptor<OperatorDomainIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _repository.GetListAsync(Filter);
        return result.Item2.ToDictionary(item => item.Domain, item => item);
    }
}