using Microsoft.Extensions.DependencyInjection;
using PointsServer.Grains;
using PointsServer.MongoDB;
using PointsServer.Worker.Worker;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace PointsServer.Worker;

[DependsOn(
    typeof(PointsServerApplicationContractsModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(AbpAutofacModule),
    typeof(PointsServerGrainsModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(PointsServerDomainModule),
    typeof(PointsServerMongoDbModule)
)]
public class PointsServerWorkerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        //var configuration = context.Services.GetConfiguration();
        context.Services.AddHostedService<PointsServerWorkerHostService>();
    }


    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        context.AddBackgroundWorkerAsync<AccumulationWorker>();
        //context.AddBackgroundWorkerAsync<PointsSettlementWorker>();
    }
}