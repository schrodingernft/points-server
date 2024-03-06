using System.Collections.Generic;

namespace PointsServer.Options;

public class MyPointsOption
{
    public List<ActionOption> ActionOptions { get; set; }
}

public class ActionOption
{
    public string Action { get; set; }
    public string DisplayName { get; set; }
    public string Symbol { get; set; }
    public decimal Amount { get; set; }
}