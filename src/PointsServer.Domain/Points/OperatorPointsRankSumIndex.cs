using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.Common;
using PointsServer.Entities;

namespace PointsServer.Points;

public class OperatorPointsRankSumIndex : PointsServerEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Domain { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string KolAddress { get; set; }
    [Keyword] public string InviterAddress { get; set; }
    public OperatorRole Role { get; set; }
    [Keyword] public string DappName { get; set; }
    [Keyword] public string FirstSymbolAmount { get; set; }
    [Keyword] public string SecondSymbolAmount { get; set; }
    [Keyword] public string ThirdSymbolAmount { get; set; }
    [Keyword] public string FourSymbolAmount { get; set; }
    [Keyword] public string FiveSymbolAmount { get; set; }
    [Keyword] public string SixSymbolAmount { get; set; } 
    [Keyword] public string SevenSymbolAmount { get; set; } 
    [Keyword] public string EightSymbolAmount { get; set; } 
    [Keyword] public string NineSymbolAmount { get; set; } 
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public long IncrementalSettlementTime { get; set; }
}

public class OperatorPointSumIndexList
{
    public long TotalCount { get; set; }
    public List<OperatorPointsRankSumIndex> IndexList { get; set; }
}