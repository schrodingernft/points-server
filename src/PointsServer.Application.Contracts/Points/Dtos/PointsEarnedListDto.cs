namespace PointsServer.Points.Dtos;

public class PointsEarnedListDto
{
    public string Domain { get; set; }
    public string Address { get; set; }
    public string DappName { get; set; }
    public string Symbol { get; set; }
    public decimal Amount { get; set; }
    public string Icon { get; set; }
}