using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Orleans;
using PointsServer.Grains.Grain.Users;
using PointsServer.Samples.Users.Eto;
using PointsServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace PointsServer.Users.Provider;

public class UserInformationProvider : IUserInformationProvider, ISingletonDependency

{
    private readonly INESTRepository<UserIndex, Guid> _userIndexRepository;
    private readonly INESTRepository<UserExtraIndex, Guid> _userExtraIndexRepository;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public UserInformationProvider(INESTRepository<UserIndex, Guid> userIndexRepository,
        INESTRepository<UserExtraIndex, Guid> userExtraIndexRepository,
        IClusterClient clusterClient, IObjectMapper objectMapper, IDistributedEventBus distributedEventBus)
    {
        _userIndexRepository = userIndexRepository;
        _userExtraIndexRepository = userExtraIndexRepository;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<UserGrainDto> GetUserById(Guid id)
    {
        var userGrain = _clusterClient.GetGrain<IUserGrain>(id);
        var resp = await userGrain.GetUserAsync();
        return resp.Success ? resp.Data : null;
    }

    public async Task<UserGrainDto> SaveUserSourceAsync(UserGrainDto userSourceInput)
    {
        var userGrain = _clusterClient.GetGrain<IUserGrain>(userSourceInput.Id);
        var result = await userGrain.UpdateUserAsync(userSourceInput);
        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<UserGrainDto, UserInformationEto>(result.Data));
        return result.Data;
    }
}