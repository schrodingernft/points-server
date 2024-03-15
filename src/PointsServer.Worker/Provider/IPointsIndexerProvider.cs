using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using PointsServer.Common.GraphQL;
using PointsServer.Worker.Provider.Dtos;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface IPointsIndexerProvider
{
    Task<List<PointsSumDto>> GetPointsSumListAsync(DateTime latestExecuteTime, DateTime nowMillisecond);

    Task<List<DomainUserRelationShipDto>>
        GetDomainUserRelationshipsAsync(DomainUserRelationShipInput relationShipInput);

    Task<List<string>> GetDomainAppliedListAsync(List<string> domainList);
}

public class PointsIndexerProvider : IPointsIndexerProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly ILogger<PointsIndexerProvider> _logger;

    public PointsIndexerProvider(IGraphQlHelper graphQlHelper, ILogger<PointsIndexerProvider> logger)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
    }

    public async Task<List<PointsSumDto>> GetPointsSumListAsync(DateTime latestExecuteTime, DateTime nowMillisecond)
    {
        var indexerResult = await _graphQlHelper.QueryAsync<PointsSumBySymbol>(new GraphQLRequest
        {
            Query =
                @"query($startTime:DateTime!,$endTime:DateTime!,$skipCount:Int!,$maxResultCount:Int!){
                    getPointsSumBySymbol(input: {startTime:$startTime,endTime:$endTime,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        totalRecordCount,
                        data {
                        id,
                        address,
                        domain,
    					role,
                        firstSymbolAmount,
                        secondSymbolAmount,
                        thirdSymbolAmount,
                        fourSymbolAmount,
                        fiveSymbolAmount,
                        sixSymbolAmount,
                        sevenSymbolAmount,
                        eightSymbolAmount,
                        nineSymbolAmount,
    					createTime,
                        updateTime
                        }
                        
                }
            }",
            Variables = new
            {
                startTime = latestExecuteTime, endTime = nowMillisecond, skipCount = 0, maxResultCount = 1000
            }
        });
        _logger.LogInformation(
            "BulkAddOrUpdateAsync, count: {count}", indexerResult.GetPointsSumBySymbol.Data.Count);
        return indexerResult.GetPointsSumBySymbol.Data;
    }

    public async Task<List<DomainUserRelationShipDto>> GetDomainUserRelationshipsAsync(
        DomainUserRelationShipInput relationShipInput)
    {
        var indexerResult = await _graphQlHelper.QueryAsync<DomainUserRelationShipQuery>(new GraphQLRequest
        {
            Query =
                @"query($domainIn:[String!]!,$addressIn:[String!]!,$dappNameIn:[String!]!,$skipCount:Int!,$maxResultCount:Int!){
                    queryUserAsync(input: {domainIn:$domainIn,addressIn:$addressIn,dappNameIn:$dappNameIn,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                        totalRecordCount
                        data {
                          id
                          domain
                          address
                          dappName
                          createTime
                        }
                }
            }",
            Variables = new
            {
                domainIn = relationShipInput.Domains, addressIn = relationShipInput.Addresses,
                dappNameIn = relationShipInput.DappNames, skipCount = 0,
                maxResultCount = 1000
            }
        });
        return indexerResult.QueryUserAsync.Data;
    }

    public async Task<List<string>> GetDomainAppliedListAsync(List<string> domainList)
    {
        var indexerResult = await _graphQlHelper.QueryAsync<DomainAppliedDto>(new GraphQLRequest
        {
            Query =
                @"query($domainList:[String!]!){
                    checkDomainApplied(input: {domainList:$domainList}){
                        domainList
                }
            }",
            Variables = new
            {
                domainList = domainList
            }
        });
        return indexerResult.CheckDomainApplied.DomainList;
    }
}