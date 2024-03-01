using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.InvitationRelationships;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface IRecordRegistrationProvider
{
    Task<List<InvitationRelationshipsIndex>> GetRecordRegistrationListAsync(long startTime, long endTime);
}

public class RecordRegistrationProvider : IRecordRegistrationProvider, ISingletonDependency
{
    private readonly INESTRepository<InvitationRelationshipsIndex, string> _repository;

    public RecordRegistrationProvider(INESTRepository<InvitationRelationshipsIndex, string> repository)
    {
        _repository = repository;
    }

    public async Task<List<InvitationRelationshipsIndex>> GetRecordRegistrationListAsync(long startTime, long endTime)
    {
        if (startTime > endTime)
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<InvitationRelationshipsIndex>, QueryContainer>>();
        mustQuery.Add(q => q.LongRange(i => i.Field(f => f.InviteTime).GreaterThanOrEquals(startTime)));
        mustQuery.Add(q => q.LongRange(i => i.Field(f => f.InviteTime).LessThan(endTime)));

        QueryContainer Filter(QueryContainerDescriptor<InvitationRelationshipsIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _repository.GetListAsync(Filter);
        return result.Item2;
    }
}