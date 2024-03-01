namespace PointsServer.Points.Dtos;

public class RankingListDto
{
   public string Domain { get; set; }
   
   public string Address { get; set; }
   public string Symbol { get; set; }
   public decimal Amount { get; set; }
   public long FollowersNumber { get; set; }
}