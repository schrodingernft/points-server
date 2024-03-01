using Nest;
using PointsServer.Common;
using PointsServer.Entities;

namespace PointsServer.Operator;

public class OperatorDomainIndex : PointsServerEntity<string>
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string InviterAddress { get; set; }
    public OperatorRole Role { get; set; }
    public ApplyStatus Status { get; set; }
    public string Domain { get; set; }
    public string Icon { get; set; }
    public string DappName { get; set; }
    public string Descibe { get; set; }
    public long ApplyTime { get; set; }
}