using Nutrix.Commons.FileSystem;
using Nutrix.Downloading;
using Nutrix.Importing;

public class ETLManager(IleWazyDownloader ileWazyDownloader, IleWazyImporter ileWazyImporter, ETLStorage storage)
{
    public async Task RunDownloader(string downloader) => await ileWazyDownloader.Download();

    public async Task RunImporter(string importer)
    {
        foreach (var path in storage.GetFilesToImport(nameof(IleWazyDownloader)))
        {
            var fileName = Path.GetFileName(path);
            var content = File.ReadAllText(path);
            await ileWazyImporter.Import(fileName, content);
            File.Delete(path);
        }
    }
}