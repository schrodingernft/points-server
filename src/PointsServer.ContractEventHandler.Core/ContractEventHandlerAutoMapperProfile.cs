using AutoMapper;
using PointsServer.Samples.Users.Eto;
using PointsServer.Users.Index;

namespace PointsServer.ContractEventHandler
{
    public class ContractEventHandlerAutoMapperProfile : Profile
    {
        public ContractEventHandlerAutoMapperProfile()
        {
            CreateMap<UserInformationEto, UserIndex>();
        }
    }
}