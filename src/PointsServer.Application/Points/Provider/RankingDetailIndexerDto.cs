using System.Collections.Generic;

namespace PointsServer.Points.Provider;

public class RankingDetailIndexerListDto
{
    public List<RankingDetailIndexerDto> IndexList { get; set; }
    public long TotalCount { get; set; }
}

public class RankingDetailIndexerDto
{
}