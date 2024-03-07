using AutoMapper;
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
        CreateMap<OperatorPointsSumIndex, RankingListDto>();
        CreateMap<GetPointsEarnedListInput, GetOperatorPointsSumIndexListByAddressInput>().ForMember(
            destination => destination.Type,
            opt => opt.MapFrom(source => source.Role));
        CreateMap<OperatorPointsSumIndex, PointsEarnedListDto>();
        CreateMap<GetPointsEarnedDetailInput, GetOperatorPointsActionSumInput>();
        CreateMap<RankingDetailIndexerDto, ActionPoints>()
            .ForMember(t => t.Action, m => m.MapFrom(f => f.ActionName))
            .ForMember(t => t.Symbol, m => m.MapFrom(f => f.PointsName))
            .ForMember(t => t.Amount, m => m.MapFrom(f => f.Amount))
            .ForMember(t => t.CreateTime, m => m.MapFrom(f => f.CreateTime.ToUtcMilliSeconds()))
            .ForMember(t => t.UpdateTime, m => m.MapFrom(f => f.UpdateTime.ToUtcMilliSeconds()))
            ;
    }
}