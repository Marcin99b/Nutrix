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
        builder.Logging.ClearProviders();
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

        builder.Logging.AddSerilog(Log.Logger);
        builder.Services.AddSingleton(Log.Logger);
        builder.Services.AddSingleton<EventLogger>();

        return builder;
    }

    public static WebApplicationBuilder SetupHangfire(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHangfire(configuration => configuration
            .UseSerilogLogProvider()
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseMemoryStorage()); //todo db
        builder.Services.AddHangfireServer();

        return builder;
    }

    public static WebApplicationBuilder SetupETL(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ETLStorage>();
        builder.Services.AddSingleton<IleWazyDownloader>();
        builder.Services.AddSingleton<IleWazyImporter>();
        builder.Services.AddSingleton<ETLManager>();
        builder.Services.AddSingleton<NutrixPaths>();
        builder.Services.AddSingleton<FileSystemProvider>();
        builder.Services.AddSingleton<DownloadHistoryFactory>();

        return builder;
    }

    public static WebApplicationBuilder SetupDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContextFactory<DatabaseContext>(options =>
            options.UseNpgsql($"Host=localhost;Username=postgres;Database=postgres"));
        builder.Services.AddSingleton<SearchProductProcedure>();
        builder.Services.AddSingleton<AddOrUpdateProductProcedure>();

        return builder;
    }
}
