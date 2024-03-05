using System;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using PointsServer.Common.GraphQL;
using PointsServer.Grains;
using PointsServer.MongoDB;
using PointsServer.Options;
using PointsServer.Worker.Options;
using PointsServer.Worker.Provider;
using PointsServer.Worker.Services;
using PointsServer.Worker.Worker;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

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
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<PointsServerWorkerModule>(); });

        var configuration = context.Services.GetConfiguration();
        Configure<WorkerOptions>(configuration.GetSection("Worker"));
        Configure<PointsCalculateOptions>(configuration.GetSection("PointsCalculate"));
        context.Services.AddHostedService<PointsServerWorkerHostService>();
        context.Services.AddSingleton<IPointsSumService, PointsSumService>();
        context.Services.AddSingleton<IApplyStatusService, ApplyStatusService>();
        context.Services.AddSingleton<IApplyStatusProvider, ApplyStatusProvider>();
        context.Services.AddSingleton<IPointsIndexerProvider, PointsIndexerProvider>();
        context.Services.AddSingleton<IPointsSumProvider, PointsSumProvider>();
        context.Services.AddSingleton<ILatestExecuteTimeProvider, LatestExecuteTimeProvider>();
        context.Services.AddSingleton<IGraphQlHelper, GraphQlHelper>();
        ConfigureGraphQl(context, configuration);
        ConfigureOrleans(context, configuration);
    }

    private void ConfigureGraphQl(ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }

    private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddSingleton(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(PointsServerGrainsModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }


    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        context.AddBackgroundWorkerAsync<CalculationWorker>();
        context.AddBackgroundWorkerAsync<PointsSumWorker>();
        context.AddBackgroundWorkerAsync<ApplyStatusWorker>();
        StartOrleans(context.ServiceProvider);
    }


    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        StopOrleans(context.ServiceProvider);
    }

    private static void StartOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async () => await client.Connect());
    }

    private static void StopOrleans(IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }
}