using System.Collections.Generic;

namespace PointsServer.Worker.Options;

public class WorkerOptions
{
    public int PointsSumPeriod { get; set; } = 600;
    public int ApplyStatusPeriod { get; set; } = 600;
}

public class DappDomainOptions
{
    public List<DappDomainDto> DappDomainList { get; set; }
}

public class DappDomainDto
{
    public string Domain { get; set; }
    public string DappId { get; set; }
}