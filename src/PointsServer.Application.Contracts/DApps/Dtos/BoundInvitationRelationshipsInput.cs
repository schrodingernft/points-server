namespace PointsServer.DApps.Dtos;

public class BoundInvitationRelationshipsInput
{
    public string Address { get; set; }
    public string DappName { get; set; }
    public string Domain { get; set; }
    public long InviteTime { get; set; }
    public string Signature { get; set; }
}