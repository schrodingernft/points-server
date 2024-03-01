using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PointsServer.Hubs;
using PointsServer.Options;
using PointsServer.Samples.Dtos;
using PointsServer.Worker.Provider;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace PointsServer.Worker;

public class ScoreDataNotificationWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IHubContext<PointsPlatformHub> _hubContext;
    private readonly IScoreDataNotificationProvider _scoreDataNotificationProvider;
    private readonly ScoreDataOptions _options;

    public ScoreDataNotificationWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IHubContext<PointsPlatformHub> hubContext, IScoreDataNotificationProvider scoreDataNotificationProvider,
        IOptionsSnapshot<ScoreDataOptions> options) : base(
        timer, serviceScopeFactory)
    {
        _hubContext = hubContext;
        _scoreDataNotificationProvider = scoreDataNotificationProvider;
        _options = options.Value;
        Timer.Period = _options.WsNotificationPeriod * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-_options.WsNotificationPeriod).ToUnixTimeMilliseconds();
        var endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var data = await _scoreDataNotificationProvider.GetOperatorPointAsync(startTime, endTime);
        await _hubContext.Clients.All.SendAsync("scoreDataNotify", new WsTestDto()
        {
            Name = "aaa",
            Age = 10,
            Data = data
        });
    }
}