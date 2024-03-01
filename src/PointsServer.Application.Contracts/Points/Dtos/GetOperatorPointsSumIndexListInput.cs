using Volo.Abp.Application.Dtos;

namespace PointsServer.Points;

public class GetOperatorPointsSumIndexListInput : PagedAndSortedResultRequestDto
{
    public string Keyword { get; set; }
    
    public string DappName { get; set; }

    public override string Sorting { get; set; } = "DESC";
}