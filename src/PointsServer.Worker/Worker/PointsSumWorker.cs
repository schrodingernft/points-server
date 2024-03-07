using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PointsServer.Worker.Options;
using PointsServer.Worker.Services;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PointsServer.Worker.Worker;

public class PointsSumWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IPointsSumService _pointsSumService;
    private readonly ILogger<PointsSumWorker> _logger;

    public PointsSumWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IPointsSumService pointsSumService, ILogger<PointsSumWorker> logger,
        IOptionsSnapshot<WorkerOptions> options) : base(timer, serviceScopeFactory)
    {
        _pointsSumService = pointsSumService;
        _logger = logger;
        Timer.Period = options.Value.PointsSumPeriod * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation(
            "begin to execute PointsSumWorker, time: {time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        await _pointsSumService.RecordPointsSumAsync();
    }
}