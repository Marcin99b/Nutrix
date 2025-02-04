using Nutrix.Downloading;
using Nutrix.Importing;

public class ETLManager(IleWazyDownloader ileWazyDownloader, IleWazyImporter ileWazyImporter)
{
    public async Task RunDownloader(string downloader, CancellationToken ct)
    {
        await ileWazyDownloader.Download(ct);
    }

    public async Task RunImporter(string importer, CancellationToken ct)
    {
        await ileWazyImporter.Import(ct);
    }
}