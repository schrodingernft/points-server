using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PointsServer.Options;
using PointsServer.Worker.Services;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PointsServer.Worker.Worker;

public class AccumulationWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<AccumulationWorker> _logger;
    private readonly IAccumulationService _accumulationService;

    public AccumulationWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<AccumulationWorker> logger, IAccumulationService accumulationService,IOptionsSnapshot<PointsCalculateOptions> options) : base(timer, serviceScopeFactory)
    {
        _logger = logger;
        _accumulationService = accumulationService;
        Timer.Period = options.Value.Period * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogInformation(
            "begin to accumulation, time: {time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

        await _accumulationService.CalculateAsync();
    }
}