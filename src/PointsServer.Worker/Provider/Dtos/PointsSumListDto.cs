using System.Collections.Generic;
using PointsServer.Common;

namespace PointsServer.Worker.Provider.Dtos;

public class PointsSumListDto
{
    public List<PointsSumDto> PointsSumList { get; set; }
}

public class PointsSumDto
{
    public string Id { get; set; }
    public string Address { get; set; }
    public string Domain { get; set; }
    public OperatorRole Role { get; set; }
    public string DappName { get; set; }
    public long FirstSymbolAmount { get; set; }
    public long SecondSymbolAmount { get; set; }
    public long ThirdSymbolAmount { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
}