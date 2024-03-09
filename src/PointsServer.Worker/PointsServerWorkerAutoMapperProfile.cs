using AutoMapper;
using PointsServer.Common;
using PointsServer.Points;
using PointsServer.Worker.Provider.Dtos;

namespace PointsServer.Worker;

public class PointsServerWorkerAutoMapperProfile : Profile
{
    public PointsServerWorkerAutoMapperProfile()
    {
        CreateMap<PointsSumDto, OperatorPointsSumIndex>()
            .ForMember(t => t.CreateTime, m => m.MapFrom(f => f.CreateTime.ToUtcMilliSeconds()))
            .ForMember(t => t.UpdateTime, m => m.MapFrom(f => f.UpdateTime.ToUtcMilliSeconds()));
        ;
    }
}