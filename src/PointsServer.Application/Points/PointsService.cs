using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PointsServer.Common;
using PointsServer.DApps;
using PointsServer.DApps.Dtos;
using PointsServer.DApps.Provider;
using PointsServer.Points.Dtos;
using PointsServer.Points.Provider;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace PointsServer.Points;

public class PointsService : IPointsService, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly IPointsRulesProvider _pointsRulesProvider;
    private readonly IPointsProvider _pointsProvider;
    private readonly ILogger<PointsService> _logger;
    private readonly IOperatorDomainProvider _operatorDomainProvider;
    private readonly IDAppService _dAppService;

    public PointsService(IObjectMapper objectMapper, IPointsProvider pointsProvider,
        IPointsRulesProvider pointsRulesProvider, IOperatorDomainProvider operatorDomainProvider,
        ILogger<PointsService> logger, IDAppService dAppService)
    {
        _objectMapper = objectMapper;
        _pointsRulesProvider = pointsRulesProvider;
        _pointsProvider = pointsProvider;
        _operatorDomainProvider = operatorDomainProvider;
        _logger = logger;
        _dAppService = dAppService;
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

        var pointsRules = await _pointsRulesProvider.GetPointsRulesAsync(input.DappName, "RegisterIncrement");
        var domains = pointsList.IndexList
            .Select(p => p.Domain).Distinct()
            .ToList();
        var kolFollowersCountDic = await _pointsProvider.GetKolFollowersCountDicAsync(domains);
        foreach (var index in pointsList.IndexList)
        {
            var dto = _objectMapper.Map<OperatorPointSumIndex, RankingListDto>(index);
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
        var items = new List<PointsEarnedListDto>();
        foreach (var operatorPointSumIndex in pointsList.IndexList)
        {
            var pointsEarnedListDto =
                _objectMapper.Map<OperatorPointSumIndex, PointsEarnedListDto>(operatorPointSumIndex);
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
        resp.PointDetails = actionPointList;

        var domainInfo = await _operatorDomainProvider.GetOperatorDomainIndexAsync(input.Domain);
        if (domainInfo != null)
        {
            resp.Describe = domainInfo.Descibe;
            resp.Icon = GetDappDto(domainInfo.DappName).Icon;
            resp.DappName = GetDappDto(domainInfo.DappName).DappName;
            resp.Domain = domainInfo.Domain;
        }

        _logger.LogInformation("GetPointsEarnedDetailAsync, resp:{req}", JsonConvert.SerializeObject(input));
        return resp;
    }

    private DAppDto GetDappDto(string dappId)
    {
        var dappIdDic = _dAppService.GetDappIdDic();
        return dappIdDic[dappId];
    }
}