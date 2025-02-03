using Nutrix.Commons.FileSystem;
using Nutrix.Downloading;
using Nutrix.Importing;

public class ETLManager(IleWazyDownloader ileWazyDownloader, IleWazyImporter ileWazyImporter)
{
    public async Task RunDownloader(string downloader) => await ileWazyDownloader.Download();

    public async Task RunImporter(string importer)
    {
        var path = NutrixPaths.GetDownloaderResult(nameof(IleWazyDownloader));
        foreach (var filePath in Directory.GetFiles(path)
            .Where(x => Path.GetFileName(x) != "DownloadHistory.json")
            .OrderBy(File.GetLastWriteTime))
        {
            var fileName = Path.GetFileName(filePath);
            var content = File.ReadAllText(filePath);
            await ileWazyImporter.Import(fileName, content);
            File.Delete(filePath);
        }
    }
}