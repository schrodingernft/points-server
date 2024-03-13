using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using PointsServer.Common;
using PointsServer.Common.GraphQL;
using PointsServer.DApps;
using PointsServer.DApps.Dtos;
using PointsServer.DApps.Provider;
using PointsServer.Options;
using PointsServer.Points.Dtos;
using PointsServer.Points.Provider;
using PointsServer.Worker.Provider.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Constants = PointsServer.Common.Constants;

namespace PointsServer.Points;

public class PointsService : IPointsService, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly IPointsRulesProvider _pointsRulesProvider;
    private readonly IPointsProvider _pointsProvider;
    private readonly ILogger<PointsService> _logger;
    private readonly IOperatorDomainProvider _operatorDomainProvider;
    private readonly IDAppService _dAppService;
    private readonly IDomainProvider _domainProvider;

    public PointsService(IObjectMapper objectMapper, IPointsProvider pointsProvider,
        IPointsRulesProvider pointsRulesProvider, IOperatorDomainProvider operatorDomainProvider,
        ILogger<PointsService> logger, IDAppService dAppService, IDomainProvider domainProvider)
    {
        _objectMapper = objectMapper;
        _pointsRulesProvider = pointsRulesProvider;
        _pointsProvider = pointsProvider;
        _operatorDomainProvider = operatorDomainProvider;
        _logger = logger;
        _dAppService = dAppService;
        _domainProvider = domainProvider;
    }

    public async Task<PagedResultDto<RankingListDto>> GetRankingListAsync(GetRankingListInput input)
    {
        _logger.LogInformation("GetRankingListAsync, req:{req}", JsonConvert.SerializeObject(input));
        var pointsList = await
            _pointsProvider.GetOperatorPointsSumIndexListAsync(
                _objectMapper.Map<GetRankingListInput, GetOperatorPointsSumIndexListInput>(input));


        var resp = new PagedResultDto<RankingListDto>();
        if (pointsList.TotalCount == 0)
        {
            return resp;
        }

        resp.TotalCount = pointsList.TotalCount;
        var items = new List<RankingListDto>();

        var pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, CommonConstant.SelfIncreaseAction);
        var domains = pointsList.IndexList
            .Select(p => p.Domain).Distinct()
            .ToList();
        var kolFollowersCountDic = await _pointsProvider.GetKolFollowersCountDicAsync(domains);
        foreach (var index in pointsList.IndexList)
        {
            var dto = _objectMapper.Map<OperatorPointsSumIndex, RankingListDto>(index);
            if (kolFollowersCountDic.TryGetValue(index.Domain, out var followersNumber))
            {
                dto.FollowersNumber = followersNumber;
            }

            dto.Rate = pointsRules.KolAmount;
            dto.Decimal = pointsRules.Decimal;
            items.Add(dto);
        }

        resp.Items = items;

        _logger.LogInformation("GetRankingListAsync, resp:{resp}", JsonConvert.SerializeObject(resp));
        return resp;
    }

    public async Task<RankingDetailDto> GetRankingDetailAsync(GetRankingDetailInput input)
    {
        _logger.LogInformation("GetRankingDetailAsync, req:{req}", JsonConvert.SerializeObject(input));
        var queryInput = _objectMapper.Map<GetRankingDetailInput, GetOperatorPointsActionSumInput>(input);
        queryInput.Role = OperatorRole.Kol;
        var actionRecordPoints = await _pointsProvider.GetOperatorPointsActionSumAsync(queryInput);

        var resp = new RankingDetailDto();
        if (actionRecordPoints.TotalRecordCount == 0)
        {
            return resp;
        }

        var actionPointList =
            _objectMapper.Map<List<RankingDetailIndexerDto>, List<ActionPoints>>(actionRecordPoints.Data);
        var kolFollowersCountDic =
            await _pointsProvider.GetKolFollowersCountDicAsync(new List<string> { input.Domain });
        var pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, CommonConstant.SelfIncreaseAction);

        foreach (var actionPoints in actionPointList.Where(actionPoints => actionPoints.Action == CommonConstant.SelfIncreaseAction))
        {
            if (kolFollowersCountDic.TryGetValue(input.Domain, out var followersNumber))
            {
                actionPoints.FollowersNumber = followersNumber;
            }

            actionPoints.Rate = pointsRules.KolAmount;
            actionPoints.Decimal = pointsRules.Decimal;
        }

        resp.PointDetails = actionPointList;

        var domainInfo = await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain);
        if (domainInfo != null)
        {
            resp.Describe = domainInfo.Descibe;
            resp.Icon = GetDappDto(domainInfo.DappName).Icon;
            resp.DappName = GetDappDto(domainInfo.DappName).DappName;
            resp.Domain = domainInfo.Domain;
        }

        _logger.LogInformation("GetRankingDetailAsync, resp:{req}", JsonConvert.SerializeObject(input));
        return resp;
    }

    public async Task<GetPointsEarnedListDto> GetPointsEarnedListAsync(GetPointsEarnedListInput input)
    {
        _logger.LogInformation("GetPointsEarnedListAsync, req:{req}", JsonConvert.SerializeObject(input));
        var queryInput =
            _objectMapper.Map<GetPointsEarnedListInput, GetOperatorPointsSumIndexListByAddressInput>(input);
        var pointsList = await _pointsProvider.GetOperatorPointsSumIndexListByAddressAsync(queryInput);

        var resp = new GetPointsEarnedListDto();
        if (pointsList.TotalCount == 0)
        {
            return resp;
        }

        resp.TotalCount = pointsList.TotalCount;

        var pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, CommonConstant.SelfIncreaseAction);
        var domains = pointsList.IndexList
            .Select(p => p.Domain).Distinct()
            .ToList();
        var kolFollowersCountDic = await _pointsProvider.GetKolFollowersCountDicAsync(domains);

        var items = new List<PointsEarnedListDto>();
        foreach (var operatorPointSumIndex in pointsList.IndexList)
        {
            var pointsEarnedListDto =
                _objectMapper.Map<OperatorPointsSumIndex, PointsEarnedListDto>(operatorPointSumIndex);

            if (kolFollowersCountDic.TryGetValue(operatorPointSumIndex.Domain, out var followersNumber))
            {
                pointsEarnedListDto.FollowersNumber = followersNumber;
            }

            pointsEarnedListDto.Rate = pointsEarnedListDto.Role == OperatorRole.Kol ? pointsRules.KolAmount : pointsRules.InviterAmount;
            pointsEarnedListDto.Decimal = pointsRules.Decimal;

            pointsEarnedListDto.DappName = GetDappDto(operatorPointSumIndex.DappName).DappName;
            pointsEarnedListDto.Icon = GetDappDto(operatorPointSumIndex.DappName).Icon;
            items.Add(pointsEarnedListDto);
        }

        resp.Items = items;


        decimal totalEarnings = 0;
        long remain = 0;
        const int maxResultCount = 20;
        queryInput.SkipCount = 0;
        do
        {
            var ret = await _pointsProvider.GetOperatorPointsSumIndexListByAddressAsync(queryInput);

            //ret.IndexList.ToList().ForEach(index => { totalEarnings += index.Amount; });

            queryInput.SkipCount += maxResultCount;
            remain = ret.TotalCount - queryInput.SkipCount;
        } while (remain > 0);

        resp.TotalEarned = totalEarnings;

        _logger.LogInformation("GetPointsEarnedListAsync, resp:{resp}", JsonConvert.SerializeObject(resp));
        return resp;
    }

    public async Task<PointsEarnedDetailDto> GetPointsEarnedDetailAsync(GetPointsEarnedDetailInput input)
    {
        _logger.LogInformation("GetPointsEarnedDetailAsync, req:{req}", JsonConvert.SerializeObject(input));
        var queryInput = _objectMapper.Map<GetPointsEarnedDetailInput, GetOperatorPointsActionSumInput>(input);
        var actionRecordPoints = await _pointsProvider.GetOperatorPointsActionSumAsync(queryInput);

        var resp = new PointsEarnedDetailDto();
        if (actionRecordPoints.TotalRecordCount == 0)
        {
            return resp;
        }

        var actionPointList =
            _objectMapper.Map<List<RankingDetailIndexerDto>, List<ActionPoints>>(actionRecordPoints.Data);
        var kolFollowersCountDic =
            await _pointsProvider.GetKolFollowersCountDicAsync(new List<string> { input.Domain });
        var pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, CommonConstant.SelfIncreaseAction);

        foreach (var actionPoints in actionPointList.Where(actionPoints => actionPoints.Action == CommonConstant.SelfIncreaseAction))
        {
            if (kolFollowersCountDic.TryGetValue(input.Domain, out var followersNumber))
            {
                actionPoints.FollowersNumber = followersNumber;
            }

            actionPoints.Rate = input.Role == OperatorRole.Kol ? pointsRules.KolAmount : pointsRules.InviterAmount;
            ;
            actionPoints.Decimal = pointsRules.Decimal;
        }

        resp.PointDetails = actionPointList;

        var domainInfo = await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain);
        if (domainInfo != null)
        {
            resp.Describe = domainInfo.Descibe;
            resp.Icon = GetDappDto(domainInfo.DappName).Icon;
            resp.DappName = GetDappDto(domainInfo.DappName).DappName;
            resp.Domain = domainInfo.Domain;
        }


        _logger.LogInformation("GetPointsEarnedDetailAsync, resp:{req}", JsonConvert.SerializeObject(resp));
        return resp;
    }

    public async Task<MyPointDetailsDto> GetMyPointsAsync(GetMyPointsInput input)
    {
        var domain = await _domainProvider.GetUserRegisterDomainByAddressAsync(input.Address);
        if (domain == null)
        {
            return new MyPointDetailsDto();
        }

        input.Domain = domain;
        _logger.LogInformation("GetMyPointsAsync, req:{req}", JsonConvert.SerializeObject(input));
        var queryInput = _objectMapper.Map<GetMyPointsInput, GetOperatorPointsActionSumInput>(input);
        queryInput.Role = OperatorRole.User;
        var actionRecordPoints = await _pointsProvider.GetOperatorPointsActionSumAsync(queryInput);

        var resp = new MyPointDetailsDto();
        if (actionRecordPoints == null || actionRecordPoints.TotalRecordCount == 0)
        {
            return resp;
        }

        var actionPointList =
            _objectMapper.Map<List<RankingDetailIndexerDto>, List<EarnedPointDto>>(actionRecordPoints.Data).OrderBy(o => o.Symbol).ToList();

        foreach (var earnedPointDto in actionPointList)
        {
            PointsRules pointsRules;
            switch (earnedPointDto.Action)
            {
                case Constants.JoinAction:
                    pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, earnedPointDto.Action);
                    if (pointsRules == null) continue;
                    earnedPointDto.Decimal = pointsRules.Decimal;
                    earnedPointDto.DisplayName = pointsRules.DisplayNamePattern;
                    break;
                case Constants.SelfIncreaseAction:
                    pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, earnedPointDto.Action);
                    if (pointsRules == null) continue;
                    earnedPointDto.Rate = pointsRules.UserAmount;
                    earnedPointDto.Decimal = pointsRules.Decimal;
                    earnedPointDto.DisplayName = pointsRules.DisplayNamePattern;
                    break;
                default:
                    pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, Constants.DefaultAction);
                    if (pointsRules == null) continue;
                    earnedPointDto.Decimal = pointsRules.Decimal;
                    earnedPointDto.DisplayName = Strings.Format(pointsRules.DisplayNamePattern, earnedPointDto.Action);
                    break;
            }
        }

        resp.PointDetails.AddRange(actionPointList);

        _logger.LogInformation("GetMyPointsAsync, resp:{resp}", JsonConvert.SerializeObject(resp));
        return resp;
    }

    private DAppDto GetDappDto(string dappId)
    {
        var dappIdDic = _dAppService.GetDappIdDic();
        return dappIdDic[dappId];
    }
}

public interface IDomainProvider
{
    Task<string> GetUserRegisterDomainByAddressAsync(string Address);
}

public class DomainProvider : IDomainProvider, ISingletonDependency
{
    private readonly IGraphQlHelper _graphQlHelper;
    private readonly ILogger<DomainProvider> _logger;

    public DomainProvider(IGraphQlHelper graphQlHelper, ILogger<DomainProvider> logger)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
    }

    public async Task<string> GetUserRegisterDomainByAddressAsync(string Address)
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
                domainIn = new List<string>(), dappNameIn = new List<string>(),
                addressIn = new List<string>() { Address }, skipCount = 0, maxResultCount = 1
            }
        });
        var ans = indexerResult.QueryUserAsync.Data;
        if (ans == null || ans.Count == 0)
        {
            return null;
        }

        return ans[0].Domain;
    }
}