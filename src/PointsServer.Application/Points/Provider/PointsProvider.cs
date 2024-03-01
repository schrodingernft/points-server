using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using PointsServer.Common;
using PointsServer.Points.Dtos;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Points.Provider;

public class PointsProvider : IPointsProvider, ISingletonDependency
{
    private readonly INESTRepository<OperatorPointSumIndex, string> _pointsSumIndexRepository;
    private readonly INESTRepository<OperatorPointActionSumIndex, string> _pointsActionSumIndexRepository;

    public PointsProvider(
        INESTRepository<OperatorPointSumIndex, string> pointsSumIndexRepository, 
        INESTRepository<OperatorPointActionSumIndex, string> pointsActionSumIndexRepository)
    {
        _pointsSumIndexRepository = pointsSumIndexRepository;
        _pointsActionSumIndexRepository = pointsActionSumIndexRepository;
    }
    
    public async Task<OperatorPointSumIndexList> GetOperatorPointsSumIndexListAsync(GetOperatorPointsSumIndexListInput input)
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
        var result = await _pointsSumIndexRepository.GetListAsync(Filter, 
            sortType: sortType, skip: input.SkipCount, limit: input.MaxResultCount);
        
        return  new OperatorPointSumIndexList
        {
            TotalCount = result.Item1,
            IndexList = result.Item2
        };
    }

    public async Task<OperatorPointActionSumIndexList> GetOperatorPointsActionSumAsync(GetOperatorPointsActionSumInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OperatorPointActionSumIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.DappName).Terms(input.DappName)));
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.Domain).Terms(input.Domain)));


        if (input.Role != null)
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.Role).Terms(input.Role)));
        }

        if (!input.Address.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.Address).Terms(input.Address)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<OperatorPointActionSumIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var result = await _pointsActionSumIndexRepository.GetListAsync(Filter, sortType: SortOrder.Descending);
        return new OperatorPointActionSumIndexList
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
        } else if (input.Type == SearchType.Operator)
        {
            mustQuery.Add(q => q.Terms(i =>
                i.Field(f => f.Role).Terms(OperatorRole.Tier2Operator)));
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
}