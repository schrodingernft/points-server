using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PointsServer.Common.GraphQL;
using PointsServer.Grains;
using PointsServer.MongoDB;
using PointsServer.Worker.Provider;
using PointsServer.Worker.Services;
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
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHostedService<PointsServerWorkerHostService>();
        context.Services.AddSingleton<IPointsSumService, PointsSumService>();
        context.Services.AddSingleton<IApplyStatusService, ApplyStatusService>();
        context.Services.AddSingleton<IApplyStatusProvider, ApplyStatusProvider>();
        context.Services.AddSingleton<IPointsIndexerProvider, PointsIndexerProvider>();
        context.Services.AddSingleton<IPointsSumProvider, PointsSumProvider>();
        context.Services.AddSingleton<ILatestExecuteTimeProvider, LatestExecuteTimeProvider>();
        context.Services.AddSingleton<IGraphQlHelper, GraphQlHelper>();
        ConfigureGraphQl(context, configuration);
    }

    private void ConfigureGraphQl(ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }


    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        //context.AddBackgroundWorkerAsync<CalculationWorker>();
        //context.AddBackgroundWorkerAsync<PointsSumWorker>();
        context.AddBackgroundWorkerAsync<ApplyStatusWorker>();
    }
}