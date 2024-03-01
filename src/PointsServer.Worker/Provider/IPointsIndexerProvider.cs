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

    Task<List<DomainUserRelationShipDto>>
        GetDomainUserRelationshipsAsync(DomainUserRelationShipInput relationShipInput);
    Task<List<string>> GetDomainAppliedListAsync(List<string> domainList);
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
                    pointsSumList(relationShipInput: {startTime:$startTime,endTime:$endTime}){
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

    public async Task<List<DomainUserRelationShipDto>> GetDomainUserRelationshipsAsync(
        DomainUserRelationShipInput relationShipInput)
    {
        var indexerResult = await _graphQlHelper.QueryAsync<DomainUserRelationShipListDto>(new GraphQLRequest
        {
            Query =
                @"query($domains:[String!],$addresses:[String!]){
                    domainUserRelationShipList(input: {domains:$domains,addresses:$addresses}){
                        id,
                        domain,
                        address,
                        dappName,
    					createTime
                }
            }",
            Variables = new
            {
                domains = relationShipInput.Domains, addresses = relationShipInput.Addresses
            }
        });
        return indexerResult.DomainUserRelationShipList;
    }

    public async Task<List<string>> GetDomainAppliedListAsync(List<string> domainList)
    {
        return await _graphQlHelper.QueryAsync<List<string>>(new GraphQLRequest
        {
            Query =
                @"query($domainList:[String!]){
                    checkDomainApplied(input: {domainList:$domainList}){
                }
            }"
        });
    }
}