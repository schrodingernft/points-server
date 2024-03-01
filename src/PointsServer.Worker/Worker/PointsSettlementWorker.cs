using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PointsServer.Worker.Services;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PointsServer.Worker.Worker;

public class PointsSettlementWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ILogger<PointsSettlementWorker> _logger;
    private readonly IPointsSettlementService _pointsSettlementService;

    public PointsSettlementWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ILogger<PointsSettlementWorker> logger, IPointsSettlementService pointsSettlementService) : base(timer,
        serviceScopeFactory)
    {
        _logger = logger;
        _pointsSettlementService = pointsSettlementService;
        Timer.Period = 10 * 60 * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _pointsSettlementService.PointsSettlementAsync();
    }
}