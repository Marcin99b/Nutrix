namespace Nutrix.Commons.FileSystem;
public class ETLStorage(NutrixPaths nutrixPaths, FileSystemProvider fileSystem)
{
    public int GetLastPage(string downloaderName)
    {
        var resultsPath = nutrixPaths.GetDownloaderResult(downloaderName);
        if (!fileSystem.Exists(resultsPath))
        {
            fileSystem.CreateDirectory(resultsPath);
        }

        var lastPage = fileSystem.GetFiles(resultsPath)
            .Select(Path.GetFileName)
            .Where(x => x != "DownloadHistory.json")
            .Select(x => x!.Split('_')[0])
            .Select(int.Parse)
            .OrderByDescending(x => x)
            .FirstOrDefault(1);
        return lastPage;
    }

    public void Save(string downloaderName, int page, string externalId, string content)
    {
        var resultsPath = nutrixPaths.GetDownloaderResult(downloaderName);
        if (!fileSystem.Exists(resultsPath))
        {
            fileSystem.CreateDirectory(resultsPath);
        }

        var fileName = $"{page}_{externalId}.html";
        fileSystem.WriteAllText(fileSystem.Combine(resultsPath, fileName), content);
    }

    public IEnumerable<string> GetFilesToImport(string downloaderName) 
    {
        var resultsPath = nutrixPaths.GetDownloaderResult(downloaderName);
        if (!fileSystem.Exists(resultsPath))
        {
            fileSystem.CreateDirectory(resultsPath);
        }

        return fileSystem.GetFiles(resultsPath).Where(x => fileSystem.GetFileName(x) != "DownloadHistory.json");
    }
}
