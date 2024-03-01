using AutoMapper;
using PointsServer.DApps.Dtos;
using PointsServer.Options;
using PointsServer.Points;
using PointsServer.Points.Dtos;
using PointsServer.Samples.Users.Dto;
using PointsServer.Samples.Users.Eto;
using PointsServer.Users;
using PointsServer.Users.Index;

namespace PointsServer;

public class PointsServerApplicationAutoMapperProfile : Profile
{
    public PointsServerApplicationAutoMapperProfile()
    {
        CreateMap<UserSourceInput, UserGrainDto>().ReverseMap();
        CreateMap<UserGrainDto, UserDto>().ReverseMap();
        CreateMap<UserGrainDto, UserInformationEto>().ReverseMap();
        CreateMap<UserIndex, UserDto>().ReverseMap();
        CreateMap<DappInfo, DAppDto>().ReverseMap();
        CreateMap<OperatorPointActionSumIndex, ActionPoints>().
            ForMember(
            destination => destination.Action,
            opt => opt.MapFrom(source => source.RecordAction)).
            ForMember(destination => destination.Symbol,
                opt => opt.MapFrom(source => source.PointSymbol));
        CreateMap<GetRankingDetailInput, GetOperatorPointsActionSumInput>();
        CreateMap<OperatorPointActionSumIndex, ActionPoints>();
        CreateMap<GetRankingListInput, GetOperatorPointsSumIndexListInput>();
        CreateMap<OperatorPointSumIndex, RankingListDto>().
            ForMember(destination => destination.Symbol,
                opt => opt.MapFrom(source => source.PointSymbol)); 
        CreateMap<GetPointsEarnedListInput, GetOperatorPointsSumIndexListByAddressInput>().
            ForMember(destination => destination.Type,
                opt => opt.MapFrom(source => source.Role)); 
        CreateMap<OperatorPointSumIndex, PointsEarnedListDto>().
            ForMember(destination => destination.Symbol,
                opt => opt.MapFrom(source => source.PointSymbol));
        CreateMap<GetPointsEarnedDetailInput, GetOperatorPointsActionSumInput>();
    }
}