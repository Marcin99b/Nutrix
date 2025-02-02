using Hangfire;
using Hangfire.Common;
using Hangfire.MemoryStorage;
using Nutrix.Commons.FileSystem;
using Nutrix.Downloader;
using Nutrix.Importing;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddHangfire(configuration => configuration
    //.UseSerilogLogProvider()
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage()); //todo db

builder.Services.AddHangfireServer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger().UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseHangfireDashboard();

var etlManager = new ETLManager();
var jobsClient = app!.Services.GetService<IRecurringJobManagerV2>()!;
jobsClient.AddOrUpdate($"Download_{nameof(IleWazyDownloader)}", () => etlManager.RunDownloader(nameof(IleWazyDownloader)), Cron.Daily);
jobsClient.AddOrUpdate($"Import_{nameof(IleWazyImporter)}", () => etlManager.RunImporter(nameof(IleWazyImporter)), Cron.Daily);

app.Run();

public class ETLManager
{
    private readonly IleWazyDownloader ileWazyDownloader = new();
    private readonly IleWazyImporter ileWazyImporter = new();
    private bool isDownloading = false;
    private bool isImporting = false;
    private readonly object obj = new();

    public async Task RunDownloader(string downloader)
    {
        try
        {
            if (isImporting)
            {
                return;
            }

            lock (obj)
            {
                isDownloading = true;
            }

            await ileWazyDownloader.Download();
        }
        finally
        {
            lock (obj)
            {
                isDownloading = false;
            }
        }
    }

    public async Task RunImporter(string importer)
    {
        try
        {
            if (isDownloading)
            {
                return;
            }

            lock (obj)
            {
                isImporting = true;
            }

            var path = NutrixPaths.GetDownloaderResult(nameof(IleWazyDownloader));
            foreach (var filePath in Directory.GetFiles(path))
            {
                var fileName = Path.GetFileName(filePath);
                var content = File.ReadAllText(filePath);
                await ileWazyImporter.Import(fileName, content);
                File.Delete(filePath);
            }
        }
        finally
        {
            lock (obj)
            {
                isImporting = false;
            }
        }
    }
}