namespace Grb.Building.Processor.Job.Infrastructure
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.SimpleNotificationService;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Be.Vlaanderen.Basisregisters.Aws.DistributedMutex;
    using Be.Vlaanderen.Basisregisters.BlobStore;
    using Be.Vlaanderen.Basisregisters.BlobStore.Aws;
    using Destructurama;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Notifications;
    using Serilog;
    using Serilog.Debugging;
    using Serilog.Extensions.Logging;

    public sealed class Program
    {
        private Program()
        { }

        public static async Task Main(string[] args)
        {
            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
                Log.Debug(
                    eventArgs.Exception,
                    "FirstChanceException event raised in {AppDomain}.",
                    AppDomain.CurrentDomain.FriendlyName);

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                Log.Fatal((Exception)eventArgs.ExceptionObject, "Encountered a fatal exception, exiting program.");

            Log.Information("Starting Grb.Building.Processor.Job.");

            var host = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, builder) =>
                {
                    builder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                        .AddJsonFile($"appsettings.{Environment.MachineName.ToLowerInvariant()}.json", optional: true, reloadOnChange: false)
                        .AddEnvironmentVariables()
                        .AddCommandLine(args);
                })
                .ConfigureLogging((hostContext, builder) =>
                {
                    SelfLog.Enable(Console.WriteLine);

                    Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(hostContext.Configuration)
                        .Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.WithThreadId()
                        .Enrich.WithEnvironmentUserName()
                        .Destructure.JsonNetTypes()
                        .CreateLogger();

                    builder.ClearProviders();
                    builder.AddSerilog(Log.Logger);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    var loggerFactory = new SerilogLoggerFactory(Log.Logger);

                    services
                        .AddDbContextFactory<BuildingGrbContext>((_, options) => options
                            .UseLoggerFactory(loggerFactory)
                            .UseSqlServer(hostContext.Configuration.GetConnectionString("BuildingGrb"), sqlServerOptions => sqlServerOptions
                                .EnableRetryOnFailure()
                                .MigrationsHistoryTable(BuildingGrbContext.MigrationsTableName, BuildingGrbContext.Schema)
                            ))
                        .AddHttpClient(nameof(BackOfficeApiProxy), client =>
                        {
                            client.BaseAddress = new Uri(hostContext.Configuration["BackOfficeApiUrl"]);
                        });

                    services.Configure<GrbApiOptions>(hostContext.Configuration);
                    services.Configure<ProcessWindowOptions>(hostContext.Configuration);

                    services.AddAWSService<IAmazonSimpleNotificationService>();
                    services.AddScoped<INotificationService>(provider =>
                    {
                        var snsService = provider.GetRequiredService<IAmazonSimpleNotificationService>();
                        var topicArn = hostContext.Configuration["TopicArn"];
                        return new NotificationService(snsService, topicArn);
                    });

                    services
                        .AddSingleton<IBlobClient>(_ => new S3BlobClient(
                            new AmazonS3Client(new AmazonS3Config
                            {
                                RegionEndpoint = hostContext.Configuration.GetAWSOptions().Region
                            }),
                            hostContext.Configuration["BucketName"]))
                        .AddSingleton<IBackOfficeApiProxy, BackOfficeApiProxy>()
                        .AddSingleton<IJobRecordsProcessor, JobRecordsProcessor>()
                        .AddSingleton<IJobRecordsMonitor, JobRecordsMonitor>()
                        .AddSingleton<IJobResultUploader>(c
                            => new JobResultUploader(
                                c.GetRequiredService<BuildingGrbContext>(),
                                c.GetRequiredService<IBlobClient>(),
                                hostContext.Configuration["ReadBuildingUrl"]))
                        .AddSingleton<IJobRecordsArchiver>(_
                            => new JobRecordsArchiver(hostContext.Configuration.GetConnectionString("BuildingGrb"), loggerFactory));

                    services.AddHostedService<JobProcessor>();
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>((hostContext, builder) =>
                {
                    var services = new ServiceCollection();
                    var loggerFactory = new SerilogLoggerFactory(Log.Logger);

                    builder
                        .RegisterModule(new BuildingGrbModule(hostContext.Configuration, services, loggerFactory))
                        .RegisterModule(new TicketingModule(hostContext.Configuration, services));

                    builder.Populate(services);
                })
                .UseConsoleLifetime()
                .Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            var configuration = host.Services.GetRequiredService<IConfiguration>();

            var notificationService = host.Services.GetRequiredService<INotificationService>();

            try
            {
                await DistributedLock<Program>.RunAsync(
                    async () =>
                    {
                        await host.RunAsync().ConfigureAwait(false);
                    },
                    DistributedLockOptions.LoadFromConfiguration(configuration),
                    logger)
                    .ConfigureAwait(false);
            }
            catch (AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    logger.LogCritical(innerException, "Encountered a fatal exception, exiting program.");
                }
            }
            catch (Exception e)
            {
                logger.LogCritical(e, "Encountered a fatal exception, exiting program.");
                Log.CloseAndFlush();

                await notificationService.PublishToTopicAsync(new NotificationMessage(
                    nameof(Job),
                    $"Encountered a fatal exception, exiting program. {Environment.NewLine}Unhandled Exception: {e.Message}",
                    "Building Import Job Processor",
                    NotificationSeverity.Danger));

                // Allow some time for flushing before shutdown.
                await Task.Delay(500, default);
                throw;
            }
            finally
            {
                logger.LogInformation("Stopping...");
            }
        }
    }
}
