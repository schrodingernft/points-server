using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Points.Contracts.Point;
using PointsServer.Common;
using PointsServer.Common.AElfSdk;
using PointsServer.Grains.State.Worker;
using PointsServer.Worker.Provider;
using Volo.Abp.DependencyInjection;

namespace PointsServer.Worker.Services;

public interface IRecordRegistrationService
{
    Task RecordRegistrationAsync();
}

public class RecordRegistrationService : IRecordRegistrationService, ISingletonDependency
{
    private readonly IRecordRegistrationProvider _recordRegistrationProvider;
    private readonly ILatestExecuteTimeProvider _latestExecuteTimeProvider;
    private readonly IContractProvider _contractProvider;


    public RecordRegistrationService(IRecordRegistrationProvider recordRegistrationProvider,
        ILatestExecuteTimeProvider latestExecuteTimeProvider, IContractProvider contractProvider)
    {
        _recordRegistrationProvider = recordRegistrationProvider;
        _latestExecuteTimeProvider = latestExecuteTimeProvider;
        _contractProvider = contractProvider;
    }

    public async Task RecordRegistrationAsync()
    {
        var nowMillisecond = DateTime.Now.Millisecond;
        var type = CommonConstant.RecordRegistrationWorker;
        var latestExecuteTime =
            await _latestExecuteTimeProvider.GetLatestExecuteTimeAsync(type);
        var recordRegistrationList =
            await _recordRegistrationProvider.GetRecordRegistrationListAsync(latestExecuteTime, nowMillisecond);

        var recordRegistrationDic = recordRegistrationList
            .GroupBy(record => new { record.DappName })
            .ToDictionary(
                group => group.Key.DappName,
                group => group.Select(item => new RegistrationRecordDetail()
                {
                    Registrant = Address.FromBase58(item.Address),
                    Domain = item.Domain,
                    CreateTime = Timestamp.FromDateTime(new DateTime(item.InviteTime))
                }).ToList()
            );

        var registrationRecordsList = recordRegistrationDic.Select(entity => new RegistrationRecords()
        {
            Service = entity.Key,
            RegistrationRecordDetail = { entity.Value }
        }).ToList();

        var recordRegistrationInput = new RecordRegistrationInput
        {
            RegistrationRecordList = new RegistrationRecordList
            {
                RegistrationRecords = { registrationRecordsList }
            }
        };

        await _contractProvider.CreateTransaction("", "", "", ContractConstant.RecordRegistration,
            recordRegistrationInput);
        await _latestExecuteTimeProvider.UpdateLatestExecuteTimeAsync(new WorkerOptionState
        {
            Type = type,
            LatestExecuteTime = nowMillisecond
        });
    }
}