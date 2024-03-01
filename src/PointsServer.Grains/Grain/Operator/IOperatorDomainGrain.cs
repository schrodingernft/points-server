using Orleans;

namespace PointsServer.Grains.Grain.Operator;

public interface IOperatorDomainGrain : IGrainWithStringKey
{
    Task<GrainResultDto<OperatorDomainGrainDto>> AddOperatorDomainAsync(OperatorDomainGrainDto dto);
}