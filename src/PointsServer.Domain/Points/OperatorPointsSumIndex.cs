using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.Common;
using PointsServer.Entities;

namespace PointsServer.Points;

public class OperatorPointsSumIndex : PointsServerEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Domain { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string KolAddress { get; set; }
    [Keyword] public string InviterAddress { get; set; }
    public OperatorRole Role { get; set; }
    [Keyword] public string DappName { get; set; }
    public long FirstSymbolAmount { get; set; }
    public long SecondSymbolAmount { get; set; }
    public long ThirdSymbolAmount { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public long IncrementalSettlementTime { get; set; }
}

public class OperatorPointSumIndexList
{
    public long TotalCount { get; set; }
    public List<OperatorPointsSumIndex> IndexList { get; set; }
}