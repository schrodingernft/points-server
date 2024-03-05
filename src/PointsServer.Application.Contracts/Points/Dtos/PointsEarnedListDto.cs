using PointsServer.Common;

namespace PointsServer.Points.Dtos;

public class PointsEarnedListDto
{
    public string Domain { get; set; }
    public string Address { get; set; }
    public string DappName { get; set; }
    public string Icon { get; set; }
    public OperatorRole Role { get; set; }
    public string FirstSymbolAmount { get; set; }
    public string SecondSymbolAmount { get; set; }
    public string ThirdSymbolAmount { get; set; }
}