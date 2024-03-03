using System.Collections.Generic;

namespace PointsServer.Points.Provider;

public class DomainUserRelationShipIndexerListDto
{
    public List<DomainUserRelationShipIndexerDto> DomainUserRelationShipList { get; set; }
}

public class DomainUserRelationShipIndexerDto
{
    public string Id { get; set; }

    public string Domain { get; set; }

    public string Address { get; set; }

    public string DappName { get; set; }

    public long CreateTime { get; set; }
}