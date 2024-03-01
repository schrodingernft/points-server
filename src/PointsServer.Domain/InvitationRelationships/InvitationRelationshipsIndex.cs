using Nest;
using PointsServer.Entities;

namespace PointsServer.InvitationRelationships;

public class InvitationRelationshipsIndex : PointsServerEntity<string>
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Address { get; set; }
    public string DappName { get; set; }
    public string Domain { get; set; }
    public string OperatorAddress { get; set; }
    public string InviterAddress { get; set; }
    public long InviteTime { get; set; }
}