namespace PointsServer.Points.Dtos;

public class RankingListDto
{
   public string Domain { get; set; }
   
   public string Address { get; set; }
   public string FirstSymbolAmount { get; set; }
   public string SecondSymbolAmount { get; set; }
   public string ThirdSymbolAmount { get; set; }
   public long FollowersNumber { get; set; }
   public long UpdateTime { get; set; }
   public long Rate { get; set; }
}