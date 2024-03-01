using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PointsServer.Apply;
using PointsServer.Apply.Dtos;
using Volo.Abp;

namespace PointsServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ApplyController")]
[Route("api/app/apply")]
[Authorize]
public class ApplyController : PointsServerController
{
    private readonly IApplyService _applyService;

    public ApplyController(IApplyService applyService)
    {
        _applyService = applyService;
    }

    [HttpPost("check")]
    public async Task<ApplyCheckResultDto> ApplyCheckAsync(ApplyCheckInput input)
    {
        return await _applyService.ApplyCheckAsync(input);
    }

    [HttpPost("confirm")]
    public async Task<ApplyConfirmDto> GetDAppListAsync(ApplyConfirmInput input)
    {
        return await _applyService.ApplyConfirmAsync(input);
    }
}