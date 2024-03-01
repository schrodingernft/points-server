using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;
using PointsServer.Grains.Grain.Worker;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Provider;

public interface ILatestExecuteTimeProvider
{
    Task<long> GetLatestExecuteTimeAsync(string type);
    Task<WorkerOptionGrainDto> UpdateLatestExecuteTimeAsync(WorkerOptionGrainDto dto);
}

public class LatestExecuteTimeProvider : ILatestExecuteTimeProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<LatestExecuteTimeProvider> _logger;

    public LatestExecuteTimeProvider(IClusterClient clusterClient, ILogger<LatestExecuteTimeProvider> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    public async Task<long> GetLatestExecuteTimeAsync(string type)
    {
        try
        {
            var workerOptionGrain = _clusterClient.GetGrain<IWorkerOptionGrain>(type);

            var result = await workerOptionGrain.GetLatestExecuteTimeAsync();

            if (!result.Success)
            {
                throw new UserFriendlyException(result.Message);
            }

            return result.Data;
        }
        catch (Exception e)
        {
            _logger.LogError("GetLatestExecuteTimeAsync error: {Message}", e.Message);
            return 0;
        }
    }

    public async Task<WorkerOptionGrainDto> UpdateLatestExecuteTimeAsync(WorkerOptionGrainDto dto)
    {
        var workerOptionGrain = _clusterClient.GetGrain<IWorkerOptionGrain>(dto.Type);

        var result = await workerOptionGrain.UpdateLatestExecuteTimeAsync(dto);

        if (!result.Success)
        {
            throw new UserFriendlyException(result.Message);
        }

        return result.Data;
    }
}