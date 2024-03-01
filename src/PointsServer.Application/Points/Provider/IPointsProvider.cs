using System.Threading.Tasks;
using PointsServer.Points.Dtos;

namespace PointsServer.Points.Provider;

public interface IPointsProvider
{
    public Task<OperatorPointSumIndexList> GetOperatorPointsSumIndexListAsync(GetOperatorPointsSumIndexListInput input);
    
    public Task<OperatorPointActionSumIndexList> GetOperatorPointsActionSumAsync(GetOperatorPointsActionSumInput input);
    
    public Task<OperatorPointSumIndexList> GetOperatorPointsSumIndexListByAddressAsync(GetOperatorPointsSumIndexListByAddressInput input);
}