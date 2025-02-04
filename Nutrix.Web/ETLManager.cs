using Nutrix.Downloading;
using Nutrix.Importing;
using Serilog.Context;

public class ETLManager(IleWazyDownloader ileWazyDownloader, IleWazyImporter ileWazyImporter)
{
    public async Task RunDownloader(string downloader)
    {
        using (LogContext.PushProperty("RunDownloader_CorrelationId", Guid.NewGuid().ToString()))
        {
            await ileWazyDownloader.Download();
        }
    }

    public async Task RunImporter(string importer)
    {
        using (LogContext.PushProperty("RunImporter_CorrelationId", Guid.NewGuid().ToString()))
        {
            await ileWazyImporter.Import();
        }
    }
}