using Hangfire;
using Hangfire.MemoryStorage;
using Nutrix.Commons.FileSystem;
using Nutrix.Downloader;
using Nutrix.Importing;

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

jobsClient.AddOrUpdate(
    $"Download_{nameof(IleWazyDownloader)}", () => etlManager.RunDownloader(nameof(IleWazyDownloader)),
    "0 3 * * 2,4"   /*At    03:00          on Tuesday and Thursday.*/);

jobsClient.AddOrUpdate(
    $"Import_{nameof(IleWazyImporter)}", () => etlManager.RunImporter(nameof(IleWazyImporter)),
    "0 4,6 * * 2,4" /*At    04:00, 06:00   on Tuesday and Thursday.*/);

app.Run();

public class ETLManager
{
    private readonly IleWazyDownloader ileWazyDownloader = new(1_000);
    private readonly IleWazyImporter ileWazyImporter = new();
    
    public async Task RunDownloader(string downloader)
    {
        await ileWazyDownloader.Download();
    }

    public async Task RunImporter(string importer)
    {
        var path = NutrixPaths.GetDownloaderResult(nameof(IleWazyDownloader));
        foreach (var filePath in Directory.GetFiles(path).OrderBy(File.GetLastWriteTime))
        {
            var fileName = Path.GetFileName(filePath);
            var content = File.ReadAllText(filePath);
            await ileWazyImporter.Import(fileName, content);
            File.Delete(filePath);
        }
    }
}