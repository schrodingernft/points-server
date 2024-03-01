using System.Threading.Tasks;

namespace PointsServer.HubsServices;

public interface IHubService
{
    Task RegisterClientAsync(string clientId, string connectionId);
    Task<string> UnRegisterClientAsync(string connectionId);
}