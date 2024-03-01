using System.Threading.Tasks;

namespace PointsServer.Points.Provider;

public interface IRelationshipProvider
{
    public Task<long> CountDomainFollowersAsync(string domain);
}