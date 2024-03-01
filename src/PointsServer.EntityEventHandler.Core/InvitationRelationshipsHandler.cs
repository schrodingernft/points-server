using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PointsServer.DApps.Etos;
using PointsServer.InvitationRelationships;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace PointsServer.EntityEventHandler.Core;

public class InvitationRelationshipsHandler : IDistributedEventHandler<InvitationRelationshipsCreateEto>,
    ITransientDependency
{
    private readonly INESTRepository<InvitationRelationshipsIndex, string> _repository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<InvitationRelationshipsHandler> _logger;

    public InvitationRelationshipsHandler(INESTRepository<InvitationRelationshipsIndex, string> contactRepository,
        IObjectMapper objectMapper, ILogger<InvitationRelationshipsHandler> logger)
    {
        _repository = contactRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(InvitationRelationshipsCreateEto eventData)
    {
        try
        {
            var contact = _objectMapper.Map<InvitationRelationshipsCreateEto, InvitationRelationshipsIndex>(eventData);

            await _repository.AddAsync(contact);
            _logger.LogDebug("add success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", JsonConvert.SerializeObject(eventData));
        }
    }
}