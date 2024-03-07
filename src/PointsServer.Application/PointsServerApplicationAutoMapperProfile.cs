using AutoMapper;
using Microsoft.VisualBasic.CompilerServices;
using PointsServer.Apply.Etos;
using PointsServer.Common;
using PointsServer.DApps.Dtos;
using PointsServer.Grains.Grain.Operator;
using PointsServer.Options;
using PointsServer.Points;
using PointsServer.Points.Dtos;
using PointsServer.Points.Provider;
using PointsServer.Users;
using PointsServer.Users.Etos;

namespace PointsServer;

public class PointsServerApplicationAutoMapperProfile : Profile
{
    public PointsServerApplicationAutoMapperProfile()
    {
        CreateMap<UserGrainDto, UserInformationEto>().ReverseMap();
        CreateMap<OperatorDomainGrainDto, OperatorDomainCreateEto>().ReverseMap();
        CreateMap<DappInfo, DAppDto>().ReverseMap();
        CreateMap<GetRankingDetailInput, GetOperatorPointsActionSumInput>();
        CreateMap<GetRankingListInput, GetOperatorPointsSumIndexListInput>();
        CreateMap<OperatorPointSumIndex, RankingListDto>();
        CreateMap<GetPointsEarnedListInput, GetOperatorPointsSumIndexListByAddressInput>().ForMember(
            destination => destination.Type,
            opt => opt.MapFrom(source => source.Role));
        CreateMap<OperatorPointSumIndex, PointsEarnedListDto>();
        CreateMap<GetPointsEarnedDetailInput, GetOperatorPointsActionSumInput>();
        CreateMap<RankingDetailIndexerDto, ActionPoints>()
            .ForMember(t => t.Action, m => m.MapFrom(f => f.ActionName))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.PointsName))
            .ForMember(t => t.Amount, m => m.MapFrom(f => f.Amount));
        CreateMap<RankingDetailIndexerDto, EarnedPointDto>()
            .ForMember(t => t.Action, m => m.MapFrom(f => f.ActionName))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.PointsName))
            .ForMember(t => t.Amount, m => m.MapFrom(f => f.Amount))
            .ForMember(t => t.UpdateTime, m => m.MapFrom(f => f.UpdateTime.ToUtcSeconds()));
    }
}