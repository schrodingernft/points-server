using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using PointsServer.Common;
using PointsServer.DApps.Dtos;
using PointsServer.Options;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace PointsServer.DApps;

public class DAppService : IDAppService
{
    private readonly IOptionsMonitor<DappOption> _dAppOption;
    private readonly IObjectMapper _objectMapper;

    public DAppService(IOptionsMonitor<DappOption> dAppOption, IObjectMapper objectMapper)
    {
        _dAppOption = dAppOption;
        _objectMapper = objectMapper;
    }

    public async Task<List<DAppDto>> GetDAppListAsync(GetDAppListInput input)
    {
        var filteredDApps = _dAppOption.CurrentValue.DappInfos
            .Where(dApp =>
                (string.IsNullOrEmpty(input.DappName) ||
                 dApp.DappName.Contains(input.DappName, StringComparison.OrdinalIgnoreCase))
                && (!input.Categories.Any() || input.Categories.Contains(dApp.Category)))
            .Select(dApp => _objectMapper.Map<DappInfo, DAppDto>(dApp))
            .ToList();

        return await Task.FromResult(filteredDApps);
    }

    public async Task<List<RoleDto>> GetRolesAsync(bool includePersonal = false)
    {
        var roles = Enum.GetValues(typeof(OperatorRole))
            .Cast<OperatorRole>()
            .Where(role => includePersonal || role != OperatorRole.User)
            .Select(role => new RoleDto { Role = role.ToString() })
            .ToList();
        return await Task.FromResult(roles);
    }

    public Dictionary<string, DAppDto> GetDappIdDic()
    {
        return _dAppOption.CurrentValue.DappInfos
            .ToDictionary(dApp => dApp.DappId, dApp => _objectMapper.Map<DappInfo, DAppDto>(dApp));
    }
}