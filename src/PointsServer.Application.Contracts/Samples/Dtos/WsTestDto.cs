using System.Collections.Generic;
using PointsServer.Points;

namespace PointsServer.Samples.Dtos;

public class WsTestDto
{
    public string Name { get; set; }
    public int Age { get; set; }
    
    public List<OperatorPointSumIndex> Data { get; set; }
}