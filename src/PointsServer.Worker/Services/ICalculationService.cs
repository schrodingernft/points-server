using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PointsServer.Common;
using PointsServer.Options;
using PointsServer.Points;
using PointsServer.Worker.Provider;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Services;

public interface ICalculationService
{
    Task CalculateAsync();
}

public class CalculationService : ICalculationService, ISingletonDependency
{
    private readonly ILogger<CalculationService> _logger;
    private readonly ICalculationProvider _calculationProvider;
    private readonly PointsCalculateOptions _options;
    private List<OperatorPointsSumIndex> _allOperatorPointSumIndices = new();

    public CalculationService(ILogger<CalculationService> logger, ICalculationProvider calculationProvider,
        IOptionsSnapshot<PointsCalculateOptions> options)
    {
        _logger = logger;
        _calculationProvider = calculationProvider;
        _options = options.Value;
    }

    public async Task CalculateAsync()
    {
        // clear operatorPointSumIndices if not empty
        _allOperatorPointSumIndices.Clear();
        _logger.LogInformation("start calculate, time:{time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));

        await GetOperatorPointSumListAsync(_allOperatorPointSumIndices, 0, _options.OnceFetchCount);
        _logger.LogInformation("total operatorPointSum count: {count}", _allOperatorPointSumIndices.Count);

        if (_allOperatorPointSumIndices.IsNullOrEmpty())
        {
            return;
        }

        _logger.LogInformation(
            "calculate config, period:{period}, updateCount:{updateCount}, onceFetchCount:{onceFetchCount}, parallelCount: {parallelCount}, decimal:{decimal}",
            _options.Period, _options.UpdateCount, _options.OnceFetchCount, _options.ParallelCount, _options.Decimal);

        CalculateScore();
        await BulkUpdatePointSumAsync();

        // clear operatorPointSumIndices
        _allOperatorPointSumIndices.Clear();
        _logger.LogInformation("finish calculate, time:{time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private void CalculateScore()
    {
        var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var users = _allOperatorPointSumIndices
            .Where(t => t.Role == OperatorRole.User && !t.KolAddress.IsNullOrEmpty() &&
                        !t.InviterAddress.IsNullOrEmpty()).ToList();
        var inviters = users.GroupBy(t => t.InviterAddress).ToList();

        //calculate inviter
        Parallel.ForEach(inviters, new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.ParallelCount
        }, (invitersGroupItem, _) => { CalculateInviter(invitersGroupItem, timeStamp); });

        CalculateEmptyVisitorScore(timeStamp);
        CalculateEmptyScore(timeStamp);
    }

    private void CalculateEmptyVisitorScore(long timeStamp)
    {
        var users = _allOperatorPointSumIndices
            .Where(t => t.Role == OperatorRole.User && !t.KolAddress.IsNullOrEmpty() &&
                        t.InviterAddress.IsNullOrEmpty()).ToList();

        var kols = users.GroupBy(t => t.KolAddress).ToList();

        // calculate kols with empty invitor
        Parallel.ForEach(kols, new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.ParallelCount
        }, (kolGroupItem, _) => { CalculateKol(kolGroupItem, timeStamp); });
    }

    private void CalculateInviter(IGrouping<string, OperatorPointsSumIndex> inviter, long timeStamp)
    {
        var allUsers = new List<OperatorPointsSumIndex>();
        long inviterInterval = 0;
        // inviter.Key
        foreach (var user in inviter)
        {
            allUsers.Add(user);
        }

        // group by to cal kol
        var kols = allUsers.GroupBy(t => t.KolAddress).ToList();
        inviterInterval += CalculateKols(kols, timeStamp);

        // get inviter and cal
        var inviterIndex = _allOperatorPointSumIndices.FirstOrDefault(t => t.Address == inviter.Key);
        if (inviterIndex == null)
        {
            _logger.LogWarning("inviter index not exist, address: {address}", inviter.Key);
            return;
        }

        inviterIndex.SecondSymbolAmount +=
            (long)(inviterInterval * _options.Coefficient.Inviter * (long)Math.Pow(10, _options.Decimal));
        inviterIndex.IncrementalSettlementTime = timeStamp;
    }

    private long CalculateKols(IEnumerable<IGrouping<string, OperatorPointsSumIndex>> kols, long timeStamp)
    {
        long inviterInterval = 0;
        foreach (var kol in kols)
        {
            long kolInterval = 0;
            // kol key
            foreach (var user in kol)
            {
                var intervalTime = CalculateUser(user, timeStamp);
                kolInterval += intervalTime;
            }

            inviterInterval += kolInterval;
            // get kol and cal
            var kolIndex = _allOperatorPointSumIndices.FirstOrDefault(t => t.Address == kol.Key);
            if (kolIndex == null)
            {
                _logger.LogWarning("kol index not exist, address: {address}", kol.Key);
                continue;
            }

            kolIndex.SecondSymbolAmount +=
                (long)(kolInterval * _options.Coefficient.Kol * (long)Math.Pow(10, _options.Decimal));
            kolIndex.IncrementalSettlementTime = timeStamp;
        }

        return inviterInterval;
    }

    private long CalculateKol(IGrouping<string, OperatorPointsSumIndex> kol, long timeStamp)
    {
        long kolInterval = 0;
        // kol key
        foreach (var user in kol)
        {
            var intervalTime = CalculateUser(user, timeStamp);
            kolInterval += intervalTime;
        }

        // get kol and cal
        var kolIndex = _allOperatorPointSumIndices.FirstOrDefault(t => t.Address == kol.Key);
        if (kolIndex == null)
        {
            _logger.LogWarning("kol index not exist, address: {address}", kol.Key);
            return 0;
        }

        kolIndex.SecondSymbolAmount +=
            (long)(kolInterval * _options.Coefficient.Kol * (long)Math.Pow(10, _options.Decimal));
        kolIndex.IncrementalSettlementTime = timeStamp;

        return kolInterval;
    }

    private void CalculateEmptyScore(long timeStamp)
    {
        var users = _allOperatorPointSumIndices.Where(t =>
            t.Role == OperatorRole.User && t.KolAddress.IsNullOrEmpty()).ToList();

        foreach (var user in users)
        {
            CalculateUser(user, timeStamp);
        }
    }

    private long CalculateUser(OperatorPointsSumIndex user, long timeStamp)
    {
        var lastAccumulateTime =
            user.IncrementalSettlementTime == 0 ? user.UpdateTime : user.IncrementalSettlementTime;

        var intervalTime = (int)((timeStamp - lastAccumulateTime) / 1000);

        user.SecondSymbolAmount +=
            (long)(intervalTime * _options.Coefficient.User * (long)Math.Pow(10, _options.Decimal));
        user.IncrementalSettlementTime = timeStamp;

        return intervalTime;
    }

    private async Task GetOperatorPointSumListAsync(List<OperatorPointsSumIndex> operatorPointSumIndices,
        int skipCount, int maxResultCount)
    {
        var operatorPointSum =
            await _calculationProvider.GetOperatorPointSumListAsync(skipCount, maxResultCount);
        operatorPointSumIndices.AddRange(operatorPointSum);

        if (operatorPointSum.Count < maxResultCount)
        {
            return;
        }

        skipCount += maxResultCount;
        await GetOperatorPointSumListAsync(operatorPointSumIndices, skipCount, maxResultCount);
    }

    private async Task BulkUpdatePointSumAsync()
    {
        _logger.LogInformation("begin to bulk update es, time:{time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        var skipCount = 0;
        var indices = _allOperatorPointSumIndices.Skip(skipCount).Take(_options.UpdateCount).ToList();

        while (!indices.IsNullOrEmpty())
        {
            await _calculationProvider.UpdateOperatorPointSumAsync(indices);

            skipCount += _options.UpdateCount;
            indices = _allOperatorPointSumIndices.Skip(skipCount).Take(_options.UpdateCount).ToList();
        }

        _logger.LogInformation("finish bulk update es, time:{time}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}