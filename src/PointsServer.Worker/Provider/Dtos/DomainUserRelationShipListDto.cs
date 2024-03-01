using System.Collections.Generic;

namespace PointsServer.Worker.Provider.Dtos;

public class DomainUserRelationShipListDto
{
    public List<DomainUserRelationShipDto> DomainUserRelationShipList { get; set; }
}

public class DomainUserRelationShipDto
{
    public string Id { get; set; }

    public string Domain { get; set; }

    public string Address { get; set; }

    public string DappName { get; set; }

    public long CreateTime { get; set; }
}