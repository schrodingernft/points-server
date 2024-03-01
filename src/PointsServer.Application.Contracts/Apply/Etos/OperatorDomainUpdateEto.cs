using PointsServer.Common;
using Volo.Abp.EventBus;

namespace PointsServer.Apply.Etos;

[EventName("OperatorDomainUpdateEto")]
public class OperatorDomainUpdateEto
{
    public string Domain { get; set; }
    public ApplyStatus Status { get; set; }
}