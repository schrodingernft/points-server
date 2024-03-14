namespace PointsServer.Points.Dtos;

public class RankingListDto
{
   public string Domain { get; set; }
   
   public string Address { get; set; }
   public string FirstSymbolAmount { get; set; }
   public string SecondSymbolAmount { get; set; }
   public string ThirdSymbolAmount { get; set; }
   public string FourSymbolAmount { get; set; }
   public string FiveSymbolAmount { get; set; }
   public string SixSymbolAmount { get; set; } 
   public string SevenSymbolAmount { get; set; } 
   public string EightSymbolAmount { get; set; } 
   public string NineSymbolAmount { get; set; } 
   public long FollowersNumber { get; set; }
   public long UpdateTime { get; set; }
   public decimal Rate { get; set; }
   public int Decimal { get; set; }
}