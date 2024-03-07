using System.Collections.Generic;

namespace PointsServer.Points.Dtos;

public class PointsEarnedDetailDto
{
    public string DappName  { get; set; }
    public string Domain  { get; set; }
    public string Address  { get; set; }
    public string Icon { get; set; }
    public string Describe { get; set; }
    public long FollowersNumber { get; set; }
    public decimal Rate { get; set; }
    public int Decimal { get; set; }
    public List<ActionPoints> PointDetails { get; set; }
}