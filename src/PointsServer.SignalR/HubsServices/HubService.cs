using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace PointsServer.HubsServices;

public class HubService : IHubService, ISingletonDependency
{
    private readonly IConnectionProvider _connectionProvider;

    public HubService(IConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public Task RegisterClientAsync(string clientId, string connectionId)
    {
        _connectionProvider.Add(clientId, connectionId);
        return Task.CompletedTask;
    }

    public Task<string> UnRegisterClientAsync(string connectionId)
    {
        var clientId = _connectionProvider.Remove(connectionId);
        return Task.FromResult(clientId);
    }
}