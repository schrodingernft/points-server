using AutoMapper;
using PointsServer.Grains.Grain.InvitationRelationships;
using PointsServer.Grains.State.InvitationRelationships;
using PointsServer.Grains.State.Users;
using PointsServer.Samples.Users.Dto;
using PointsServer.Samples.Users.Eto;
using PointsServer.Users;

namespace PointsServer.Grains;

public class SymbolMarketGrainsAutoMapperProfile : Profile
{
    public SymbolMarketGrainsAutoMapperProfile()
    {
        CreateMap<UserGrainDto, UserState>().ReverseMap();
        CreateMap<UserGrainDto, UserDto>().ReverseMap();
        CreateMap<UserGrainDto, UserInformationEto>().ReverseMap();
        CreateMap<InvitationRelationshipsState, InvitationRelationshipsGrainDto>().ReverseMap();
    }
}