using System;
using System.Collections.Generic;
using PointsServer.Common;

namespace PointsServer.Worker.Provider.Dtos;

public class PointsSumBySymbol
{
    public PointsSumListDto GetPointsSumBySymbol { get; set; }

}

public class PointsSumListDto
{
    public List<PointsSumDto> Data { get; set; }
    public long TotalRecordCount { get; set; }
}

public class PointsSumDto
{
    public string Id { get; set; }
    public string Address { get; set; }
    public string Domain { get; set; }
    public OperatorRole Role { get; set; }
    public long FirstSymbolAmount { get; set; }
    public long SecondSymbolAmount { get; set; }
    public long ThirdSymbolAmount { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}