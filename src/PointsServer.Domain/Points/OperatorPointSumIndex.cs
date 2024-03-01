using System;
using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.Common;
using PointsServer.Entities;

namespace PointsServer.Points;

public class OperatorPointSumIndex : PointsServerEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    public string Domain { get; set; }
    [Keyword] public string Address { get; set; }
    public OperatorRole Role { get; set; }
    public string DappName { get; set; }
    public string Icon { get; set; }
    public decimal Amount { get; set; }
    public string PointSymbol { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}

public class OperatorPointSumIndexList
{
    public long TotalCount { get; set; }
    public List<OperatorPointSumIndex> IndexList { get; set; }
}