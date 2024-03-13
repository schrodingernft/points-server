using System.Collections.Generic;

namespace PointsServer.Worker.Provider.Dtos;

public class DomainAppliedDto
{
    public CheckDomainApplied CheckDomainApplied { get; set; }
}

public class CheckDomainApplied
{
    public List<string> DomainList { get; set; }
}