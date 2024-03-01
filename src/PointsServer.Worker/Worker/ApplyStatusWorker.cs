using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PointsServer.Worker.Services;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PointsServer.Worker.Worker;

public class ApplyStatusWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IApplyStatusService _applyStatusService;
    private readonly ILogger<PointsSumWorker> _logger;

    public ApplyStatusWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IApplyStatusService applyStatusService, ILogger<PointsSumWorker> logger) : base(timer, serviceScopeFactory)
    {
        _applyStatusService = applyStatusService;
        _logger = logger;
        Timer.Period = 3 * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _applyStatusService.ApplyStatusChangeAsync();
    }
}