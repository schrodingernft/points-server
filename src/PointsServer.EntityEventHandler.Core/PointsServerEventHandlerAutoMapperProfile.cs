using AutoMapper;
using PointsServer.Apply.Etos;
using PointsServer.Operator;
using PointsServer.Users.Etos;
using PointsServer.Users.Index;

namespace PointsServer.EntityEventHandler.Core;

public class PointsServerEventHandlerAutoMapperProfile : Profile
{
    public PointsServerEventHandlerAutoMapperProfile()
    {
        CreateMap<OperatorDomainCreateEto, OperatorDomainIndex>();
        CreateMap<OperatorDomainUpdateEto, OperatorDomainIndex>();
        CreateMap<UserInformationEto, UserIndex>();
    }
}