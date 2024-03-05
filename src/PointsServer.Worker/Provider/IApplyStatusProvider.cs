using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using Orleans;
using PointsServer.Apply.Etos;
using PointsServer.Common;
using PointsServer.Grains.Grain.Operator;
using PointsServer.Operator;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace PointsServer.Worker.Provider;

public interface IApplyStatusProvider
{
    Task<List<OperatorDomainIndex>> GetApplyingListAsync();
    Task BatchUpdateApplyStatusAsync(List<string> domainAppliedList);
}

public class ApplyStatusProvider : IApplyStatusProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorDomainIndex, string> _repository;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;

    public ApplyStatusProvider(INESTRepository<OperatorDomainIndex, string> repository, IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, IObjectMapper objectMapper)
    {
        _repository = repository;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
    }

    public async Task<List<OperatorDomainIndex>> GetApplyingListAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorDomainIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Status).Value(ApplyStatus.Applying)));

        QueryContainer Filter(QueryContainerDescriptor<OperatorDomainIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _repository.GetListAsync(Filter);
        return result.Item2;
    }

    public async Task BatchUpdateApplyStatusAsync(List<string> domainAppliedList)
    {
        var tasks = domainAppliedList.Select(UpdateApplyStatus);
        await Task.WhenAll(tasks);
    }

    private async Task UpdateApplyStatus(string domain)
    {
        var operatorDomainGrain = _clusterClient.GetGrain<IOperatorDomainGrain>(domain);
        await operatorDomainGrain.UpdateApplyStatusAsync(ApplyStatus.Applied);

        await _distributedEventBus.PublishAsync(new OperatorDomainUpdateEto()
        {
            Id = domain,
            Domain = domain,
            Status = ApplyStatus.Applied
        });
    }
}