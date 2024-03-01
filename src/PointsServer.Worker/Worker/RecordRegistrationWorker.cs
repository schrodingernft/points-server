using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PointsServer.Worker.Services;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PointsServer.Worker.Worker;

public class RecordRegistrationWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IRecordRegistrationService _registrationService;

    public RecordRegistrationWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IRecordRegistrationService registrationService) : base(timer, serviceScopeFactory)
    {
        _registrationService = registrationService;
        Timer.Period = 10 * 60 * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _registrationService.RecordRegistrationAsync();
    }
}