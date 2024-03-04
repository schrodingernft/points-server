using System.Collections.Generic;
using System.Threading.Tasks;
using PointsServer.DApps.Dtos;

namespace PointsServer.DApps;

public interface IDAppService
{
    Task<List<DAppDto>> GetDAppListAsync(GetDAppListInput input);
    Task<List<RoleDto>> GetRolesAsync(bool includePersonal = false);
}