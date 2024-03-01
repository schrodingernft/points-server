using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.InvitationRelationships;
using Volo.Abp.DependencyInjection;

namespace PointsServer.DApps.Provider;

public interface IInvitationRelationshipsProvider
{
    Task<InvitationRelationshipsIndex> GetInvitationRelationshipsAsync(string address); 
    Task<long> CountDomainFollowersAsync(string domain);
}

public class InvitationRelationshipsProvider : IInvitationRelationshipsProvider, ISingletonDependency
{
    private readonly INESTRepository<InvitationRelationshipsIndex, string> _repository;

    public InvitationRelationshipsProvider(INESTRepository<InvitationRelationshipsIndex, string> repository)
    {
        _repository = repository;
    }

    public async Task<InvitationRelationshipsIndex> GetInvitationRelationshipsAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<InvitationRelationshipsIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Address)
            .Terms(address)));

        QueryContainer Filter(QueryContainerDescriptor<InvitationRelationshipsIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return await _repository.GetAsync(Filter);
    }
    
    public async Task<long> CountDomainFollowersAsync(string domain)
    {
        if (domain == "")
        {
            return 0;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<InvitationRelationshipsIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Domain)
            .Value(domain)));

        QueryContainer Filter(QueryContainerDescriptor<InvitationRelationshipsIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var countResp =
            await _repository.CountAsync(Filter);

        return countResp.Count;
    }
}