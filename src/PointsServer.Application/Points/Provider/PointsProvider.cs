using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Nest;
using PointsServer.Common;
using PointsServer.Common.GraphQL;
using PointsServer.Points.Dtos;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Points.Provider;

public interface IPointsProvider
{
    public Task<OperatorPointSumIndexList> GetOperatorPointsSumIndexListAsync(GetOperatorPointsSumIndexListInput input);

    public Task<OperatorPointSumIndexList> GetOperatorPointsSumIndexListByAddressAsync(
        GetOperatorPointsSumIndexListByAddressInput input);

    public Task<Dictionary<string, long>> GetKolFollowersCountDicAsync(List<string> domainList);

    Task<RankingDetailIndexerListDto> GetOperatorPointsActionSumAsync(GetOperatorPointsActionSumInput queryInput);
}

public class PointsProvider : IPointsProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorPointSumIndex, string> _pointsSumIndexRepository;
    private readonly IGraphQlHelper _graphQlHelper;

    public PointsProvider(
        INESTRepository<OperatorPointSumIndex, string> pointsSumIndexRepository, IGraphQlHelper graphQlHelper)
    {
        _pointsSumIndexRepository = pointsSumIndexRepository;
        _graphQlHelper = graphQlHelper;
    }

    public async Task<OperatorPointSumIndexList> GetOperatorPointsSumIndexListAsync(
        GetOperatorPointsSumIndexListInput input)
    {
        var shouldQuery1 = new List<Func<QueryContainerDescriptor<OperatorPointSumIndex>, QueryContainer>>();
        var shouldQuery2 = new List<Func<QueryContainerDescriptor<OperatorPointSumIndex>, QueryContainer>>();

        if (!input.Keyword.IsNullOrWhiteSpace())
        {
            shouldQuery1.Add(q => q.Terms(i =>
                i.Field(f => f.Domain).Terms(input.Keyword)));
            shouldQuery2.Add(q => q.Terms(i =>
                i.Field(f => f.Address).Terms(input.Keyword)));
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorPointSumIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.DappName).Terms(input.DappName)));

        QueryContainer Filter(QueryContainerDescriptor<OperatorPointSumIndex> f)
            => f.Bool(b =>
                b.MinimumShouldMatch(1)
                    .Should(shouldQuery1)
                    .Should(shouldQuery2)
            );

        var sortType = input.Sorting == "DESC" ? SortOrder.Descending : SortOrder.Ascending;
        var result = await _pointsSumIndexRepository.GetListAsync(Filter, sortType: sortType,
            sortExp: GetSortBy(input.SortingKeyWord), skip: input.SkipCount, limit: input.MaxResultCount);

        return new OperatorPointSumIndexList
        {
            TotalCount = result.Item1,
            IndexList = result.Item2
        };
    }

    public async Task<OperatorPointSumIndexList> GetOperatorPointsSumIndexListByAddressAsync(
        GetOperatorPointsSumIndexListByAddressInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorPointSumIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.Address).Terms(input.Address)));
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.DappName).Terms(input.DappName)));

        if (input.Type == SearchType.Inviter)
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.Role).Terms(OperatorRole.Inviter)));
        }
        else if (input.Type == SearchType.Operator)
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.Role).Terms(OperatorRole.Kol)));
        }

        QueryContainer Filter(QueryContainerDescriptor<OperatorPointSumIndex> f) => f.Bool(b => b.Must(mustQuery));


        var sortType = input.Sorting == "DESC" ? SortOrder.Descending : SortOrder.Ascending;
        var result = await _pointsSumIndexRepository.GetListAsync(Filter, sortType: sortType);
        return new OperatorPointSumIndexList
        {
            TotalCount = result.Item1,
            IndexList = result.Item2
        };
    }

    public async Task<Dictionary<string, long>> GetKolFollowersCountDicAsync(List<string> domainList)
    {
        var indexerResult = await _graphQlHelper.QueryAsync<DomainUserRelationShipIndexerQuery>(new GraphQLRequest
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
                domainIn = domainList, addressIn = new List<string>(), dappNameIn = new List<string>(), skipCount = 0,
                maxResultCount = 1000
            }
        });

        var result = indexerResult.QueryUserAsync.Data;
        if (result.IsNullOrEmpty())
        {
            return new Dictionary<string, long>();
        }

        return result
            .GroupBy(a => a.Domain)
            .ToDictionary(a => a.Key, a => (long)a.Count());
    }

    public async Task<RankingDetailIndexerListDto> GetOperatorPointsActionSumAsync(
        GetOperatorPointsActionSumInput queryInput)
    {
        var indexerResult = await _graphQlHelper.QueryAsync<RankingDetailIndexerListDto>(new GraphQLRequest
        {
            Query =
                @"query($domains:[String!]){
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
                domains = ""
            }
        });

        return indexerResult;
    }

    private Expression<Func<OperatorPointSumIndex, object>> GetSortBy(SortingKeywordType sortingKeyWord)
    {
        return sortingKeyWord switch
        {
            SortingKeywordType.FirstSymbolAmount => a => a.FirstSymbolAmount,
            SortingKeywordType.SecondSymbolAmount => a => a.SecondSymbolAmount,
            SortingKeywordType.ThirdSymbolAmount => a => a.ThirdSymbolAmount,
            _ => a => a.FirstSymbolAmount
        };
    }
}