
using Nutrix.Commons.ETL;
using Nutrix.Database.Procedures;
using Nutrix.Importing;
using System.Threading.Channels;

namespace Nutrix.Web.Background;

public class ImportingBackgroundService(Channel<ImportRequest> channel, IleWazyImporter importer, AddOrUpdateProductProcedure addOrUpdate) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach(var request in channel.Reader.ReadAllAsync(stoppingToken)) 
        {
            var result = importer.Import(request);
            await addOrUpdate.Execute(result, stoppingToken);
        }
    }
}
