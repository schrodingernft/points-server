using System.Collections.Generic;

namespace PointsServer.Options;

public class PointsRulesOption
{
    public List<PointsRules> PointsRulesList { get; set; }
}

public class PointsRules
{
    public string DappName { get; set; }
    public string Action { get; set; }
    public string Symbol { get; set; }
    public decimal Amount { get; set; }
    public string PercentageOfTier2Operator { get; set; }
    public string PercentageOfInviter { get; set; }
}