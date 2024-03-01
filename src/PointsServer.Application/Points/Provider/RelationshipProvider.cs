using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.InvitationRelationships;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Points.Provider;

public class RelationshipProvider : IRelationshipProvider, ISingletonDependency
{
    private readonly INESTRepository<InvitationRelationshipsIndex, string> _invitationRelationshipsIndexRepository;
    
    public RelationshipProvider(
        INESTRepository<InvitationRelationshipsIndex, string> invitationRelationshipsIndexRepository)
    {
        _invitationRelationshipsIndexRepository = invitationRelationshipsIndexRepository;
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
            await _invitationRelationshipsIndexRepository.CountAsync(Filter);

        return countResp.Count;
    }
}