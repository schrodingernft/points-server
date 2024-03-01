using System.Threading.Tasks;
using PointsServer.Apply.Dtos;

namespace PointsServer.Apply;

public interface IApplyService
{
    Task<ApplyCheckResultDto> ApplyCheckAsync(ApplyCheckInput input);
    Task<ApplyConfirmDto> ApplyConfirmAsync(ApplyConfirmInput input);
}