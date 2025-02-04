using Nutrix.Downloading;
using Nutrix.Importing;

public class ETLManager(IleWazyDownloader ileWazyDownloader, IleWazyImporter ileWazyImporter)
{
    public async Task RunDownloader(string downloader)
    {
        await ileWazyDownloader.Download();
    }

    public async Task RunImporter(string importer)
    {
        await ileWazyImporter.Import();
    }
}