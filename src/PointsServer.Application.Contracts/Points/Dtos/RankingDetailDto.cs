using System.Collections.Generic;

namespace PointsServer.Points.Dtos;

public class RankingDetailDto
{
    public string DappName  { get; set; }
    public string Domain  { get; set; }
    public string Icon { get; set; }
    public string Describe { get; set; }
    public long FollowersNumber { get; set; }
    public decimal Rate { get; set; }
    public int Decimal { get; set; }
    public List<ActionPoints> PointDetails { get; set; }
}

public class ActionPoints
{
    public string Action { get; set; }
    public string Symbol { get; set; }
    public decimal Amount { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
}