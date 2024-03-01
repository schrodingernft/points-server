using Volo.Abp.Application.Dtos;

namespace PointsServer.Points.Dtos;

public class GetOperatorPointsSumIndexListByAddressInput : PagedAndSortedResultRequestDto
{
    public override string Sorting { get; set; } = "DESC";
    
    public string Address { get; set; }
    
    public string DappName { get; set; }

    public SearchType Type { get; set; }
}