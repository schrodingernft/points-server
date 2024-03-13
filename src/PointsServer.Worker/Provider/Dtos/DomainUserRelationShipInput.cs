using System.Collections.Generic;

namespace PointsServer.Worker.Provider.Dtos;

public class DomainUserRelationShipInput
{
    public List<string> Domains { get; set; } = new();
    public List<string> Addresses { get; set; } = new();
    public List<string> DappNames { get; set; } = new();
}