using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PointsServer.Worker.Provider;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Services;

public interface IAccumulationService
{
    Task AccumulationAsync();
}

public class AccumulationService : IAccumulationService, ISingletonDependency
{
    private readonly ILogger<AccumulationService> _logger;
    private readonly IAccumulationProvider _accumulationProvider;

    public AccumulationService(ILogger<AccumulationService> logger, IAccumulationProvider accumulationProvider)
    {
        _logger = logger;
        _accumulationProvider = accumulationProvider;
    }

    public async Task AccumulationAsync()
    {
        _logger.LogInformation("in AccumulationAsync");

         var list = await _accumulationProvider.GetOperatorDomainListAsync(0, 1000);
        // for (int i = 0; i < list.Count; i += 10)
        // {
        //     List<Task<string>> tasks = new List<Task<string>>(10);
        //     // whenall,  publish(result)
        // }

  
    }
}