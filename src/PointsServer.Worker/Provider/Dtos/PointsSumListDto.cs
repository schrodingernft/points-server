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
    public string FirstSymbolAmount { get; set; }
    public string SecondSymbolAmount { get; set; }
    public string ThirdSymbolAmount { get; set; }
    public string FourSymbolAmount { get; set; }
    public string FiveSymbolAmount { get; set; }
    public string SixSymbolAmount { get; set; } 
    public string SevenSymbolAmount { get; set; } 
    public string EightSymbolAmount { get; set; } 
    public string NineSymbolAmount { get; set; } 
    
    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }
}