
using Nutrix.Commons.ETL;
using Nutrix.Database.Procedures;
using Nutrix.Importing;
using System.Threading.Channels;

namespace Nutrix.Web.Background;

public class ImportingBackgroundService(
    Channel<ImportRequest> channel, 
    IServiceProvider serviceProvider, 
    AddOrUpdateProductProcedure addOrUpdate) 
    : BackgroundService
{
    private readonly int chunkCapacity = 1000;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var chunk = new List<ImportRequest>(this.chunkCapacity);
        var lastSave = DateTime.Now;
        await foreach(var request in channel.Reader.ReadAllAsync(ct))
        {
            chunk.Add(request);
            if (chunk.Count == this.chunkCapacity || (DateTime.Now - lastSave).TotalMinutes >= 5)
            {
                var imported = chunk
                    .GroupBy(x => x.Source)
                    .SelectMany(x => 
                    {
                        var source = x.First().Source;
                        var importer = serviceProvider.GetKeyedService<IImporter>(DownloaderSources.IleWazy)!;
                        return x.Select(importer.Import);
                    });

                await addOrUpdate.Execute(imported, ct);
                chunk.Clear();
                lastSave = DateTime.Now;
            }
        }
    }
}
