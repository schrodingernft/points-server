using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using PointsServer.Common.GraphQL;
using PointsServer.Worker.Provider.Dtos;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface IPointsIndexerProvider
{
    Task<List<PointsSumDto>> GetPointsSumListAsync(long latestExecuteTime, int nowMillisecond);
}

public class PointsIndexerProvider : IPointsIndexerProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;

    public PointsIndexerProvider(IGraphQlHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<List<PointsSumDto>> GetPointsSumListAsync(long latestExecuteTime, int nowMillisecond)
    {
        var indexerResult = await _graphQlHelper.QueryAsync<PointsSumListDto>(new GraphQLRequest
        {
            Query =
                @"query($startTime:String!,$endTime:Int!){
                    pointsSumList(input: {startTime:$startTime,endTime:$endTime}){
                        id,
                        address,
                        domain,
    					role,
                        dappName,
                        firstSymbolAmount,
                        secondSymbolAmount,
                        thirdSymbolAmount,
    					createTime,
                        updateTime
                }
            }",
            Variables = new
            {
                startTime = latestExecuteTime, endTime = nowMillisecond
            }
        });
        return indexerResult.PointsSumList;
    }
}