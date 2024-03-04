namespace PointsServer.Options;

public class PointsCalculateOptions
{
    // second
    public int Period { get; set; } = 600;
    public int UpdateCount { get; set; } = 1000;
    public int OnceFetchCount { get; set; } = 5000;
    public int ParallelCount { get; set; } = System.Environment.ProcessorCount;
    public int Decimal { get; set; }
    public Coefficient Coefficient { get; set; }
}

public class Coefficient
{
    public decimal User { get; set; }
    public decimal Kol { get; set; }
    public decimal Inviter { get; set; }
}