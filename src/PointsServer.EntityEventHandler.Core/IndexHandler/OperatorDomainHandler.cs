using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using PointsServer.Apply.Etos;
using PointsServer.Operator;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace PointsServer.EntityEventHandler.Core.IndexHandler;

public class OperatorDomainHandler : IDistributedEventHandler<OperatorDomainCreateEto>,
    IDistributedEventHandler<OperatorDomainUpdateEto>, ITransientDependency
{
    private readonly INESTRepository<OperatorDomainIndex, string> _operatorDomainRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<OperatorDomainHandler> _logger;

    public OperatorDomainHandler(INESTRepository<OperatorDomainIndex, string> operatorDomainRepository,
        IObjectMapper objectMapper,
        ILogger<OperatorDomainHandler> logger)
    {
        _operatorDomainRepository = operatorDomainRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }


    public async Task HandleEventAsync(OperatorDomainCreateEto eventData)
    {
        
    }

    public async Task HandleEventAsync(OperatorDomainUpdateEto eventData)
    {
        try
        {
            var operatorDomainIndex =
                _objectMapper.Map<OperatorDomainUpdateEto, OperatorDomainIndex>(eventData);

            await _operatorDomainRepository.UpdateAsync(operatorDomainIndex);

            _logger.LogDebug("OperatorDomain information update success: {domain}", operatorDomainIndex.Domain);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OperatorDomain information update fail: {domain}", eventData.Domain);
        }
    }
}