using AutoMapper;
using PointsServer.DApps.Dtos;
using PointsServer.Options;
using PointsServer.Points;
using PointsServer.Points.Dtos;
using PointsServer.Users;
using PointsServer.Users.Etos;

namespace PointsServer;

public class PointsServerApplicationAutoMapperProfile : Profile
{
    public PointsServerApplicationAutoMapperProfile()
    {
        CreateMap<UserGrainDto, UserInformationEto>().ReverseMap();
        CreateMap<DappInfo, DAppDto>().ReverseMap();
        CreateMap<GetRankingDetailInput, GetOperatorPointsActionSumInput>();
        CreateMap<GetRankingListInput, GetOperatorPointsSumIndexListInput>();
        CreateMap<OperatorPointSumIndex, RankingListDto>();
        CreateMap<GetPointsEarnedListInput, GetOperatorPointsSumIndexListByAddressInput>().ForMember(
            destination => destination.Type,
            opt => opt.MapFrom(source => source.Role));
        CreateMap<OperatorPointSumIndex, PointsEarnedListDto>();
        CreateMap<GetPointsEarnedDetailInput, GetOperatorPointsActionSumInput>();
    }
}