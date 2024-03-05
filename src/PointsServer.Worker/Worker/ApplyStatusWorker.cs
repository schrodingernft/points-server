using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PointsServer.Worker.Options;
using PointsServer.Worker.Services;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PointsServer.Worker.Worker;

public class ApplyStatusWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IApplyStatusService _applyStatusService;
    private readonly ILogger<PointsSumWorker> _logger;

    public ApplyStatusWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IApplyStatusService applyStatusService, ILogger<PointsSumWorker> logger,
        IOptionsSnapshot<WorkerOptions> options) : base(timer, serviceScopeFactory)
    {
        _applyStatusService = applyStatusService;
        _logger = logger;
        Timer.Period = options.Value.ApplyStatusPeriod * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _applyStatusService.ApplyStatusChangeAsync();
    }
}