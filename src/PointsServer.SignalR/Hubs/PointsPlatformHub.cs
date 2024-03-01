using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;

namespace PointsServer.Hubs;

[HubRoute("points-platform")]
public class PointsPlatformHub : AbpHub
{
    private readonly ILogger<PointsPlatformHub> _logger;

    public PointsPlatformHub(ILogger<PointsPlatformHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("user connected, connectId:{connectId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public async Task Connect(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        // await _hubService.RegisterClient(clientId, Context.ConnectionId);
        _logger.LogInformation("clientId={clientId} connect", clientId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("disconnected: {connectionId}", Context.ConnectionId);
        return Task.CompletedTask;
    }
}