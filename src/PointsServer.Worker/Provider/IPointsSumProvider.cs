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
    Task<Dictionary<string, OperatorDomainInfoIndex>> GetKolInviterRelationShipByDomainsAsync(List<string> domains);
}

public class PointsSumProvider : IPointsSumProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorDomainInfoIndex, string> _repository;

    public PointsSumProvider(INESTRepository<OperatorDomainInfoIndex, string> repository)
    {
        _repository = repository;
    }

    public async Task<Dictionary<string, OperatorDomainInfoIndex>> GetKolInviterRelationShipByDomainsAsync(List<string> domains)
    {
        if (domains.IsNullOrEmpty())
        {
            return new Dictionary<string, OperatorDomainInfoIndex>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorDomainInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Domain).Terms(domains)));

        QueryContainer Filter(QueryContainerDescriptor<OperatorDomainInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _repository.GetListAsync(Filter);
        return result.Item2.ToDictionary(item => item.Domain, item => item);
    }
}