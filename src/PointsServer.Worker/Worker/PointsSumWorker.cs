using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PointsServer.Worker.Services;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PointsServer.Worker.Worker;

public class PointsSumWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IPointsSumService _pointsSumService;
    private readonly ILogger<PointsSumWorker> _logger;
    
    public PointsSumWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IPointsSumService pointsSumService, ILogger<PointsSumWorker> logger) : base(timer, serviceScopeFactory)
    {
        _pointsSumService = pointsSumService;
        _logger = logger;
        Timer.Period = 3 * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _pointsSumService.RecordPointsSumAsync();
    }
}