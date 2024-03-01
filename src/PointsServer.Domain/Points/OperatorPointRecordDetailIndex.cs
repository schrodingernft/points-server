using Nest;
using PointsServer.Common;
using PointsServer.Entities;

namespace PointsServer.Points;

public class OperatorPointRecordDetailIndex  : PointsServerEntity<string>
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Address { get; set; }
    public OperatorRole Role { get; set; }
    public string Domain { get; set; }
    public string DappName { get; set; }
    public string RecordAction { get; set; }  
    public decimal Amount { get; set; }
    public string PointSymbol { get; set; }  
    public long RecordTime { get; set; }
}