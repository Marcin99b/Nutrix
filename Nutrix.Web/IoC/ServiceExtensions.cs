using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Nutrix.Commons.FileSystem;
using Nutrix.Database;
using Nutrix.Database.Procedures;
using Nutrix.Downloading;
using Nutrix.Importing;
using Nutrix.Logging;
using Serilog;

namespace Nutrix.Web.IoC;

public static class ServiceExtensions
{
    public static WebApplicationBuilder SetupLogging(this WebApplicationBuilder builder)
    {
        _ = builder.Logging.ClearProviders();
        var openObserveEmail = Environment.GetEnvironmentVariable("openobserve_login", EnvironmentVariableTarget.User);
        var openObservePassword = Environment.GetEnvironmentVariable("openobserve_password", EnvironmentVariableTarget.User);
        Log.Logger = new LoggerConfiguration()
            .Enrich.WithEnvironmentName()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.OpenObserve("http://localhost:5080", "default", openObserveEmail, openObservePassword, "logs").MinimumLevel.Information()
            .WriteTo.Console().MinimumLevel.Information()
            .CreateLogger();

        _ = builder.Logging.AddSerilog(Log.Logger);
        _ = builder.Services.AddSingleton(Log.Logger);
        _ = builder.Services.AddSingleton<EventLogger>();

        return builder;
    }

    public static WebApplicationBuilder SetupHangfire(this WebApplicationBuilder builder)
    {
        _ = builder.Services
            .AddHangfire(configuration => configuration
            .UseSerilogLogProvider()
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseMemoryStorage()); //todo db
        _ = builder.Services.AddHangfireServer();

        return builder;
    }

    public static WebApplicationBuilder SetupETL(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddSingleton<ETLStorage>();
        _ = builder.Services.AddSingleton<IleWazyDownloader>();
        _ = builder.Services.AddSingleton<IleWazyImporter>();
        _ = builder.Services.AddSingleton<ETLManager>();
        _ = builder.Services.AddSingleton<NutrixPaths>();
        _ = builder.Services.AddSingleton<FileSystemProvider>();
        _ = builder.Services.AddSingleton<DownloadHistoryFactory>();

        return builder;
    }

    public static WebApplicationBuilder SetupDatabase(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddDbContextFactory<DatabaseContext>(options =>
            options.UseNpgsql($"Host=localhost;Username=postgres;Database=postgres"));
        _ = builder.Services.AddSingleton<SearchProductProcedure>();
        _ = builder.Services.AddSingleton<AddOrUpdateProductProcedure>();

        return builder;
    }
}
