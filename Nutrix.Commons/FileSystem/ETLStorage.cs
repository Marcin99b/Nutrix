using System.IO;

namespace Nutrix.Commons.FileSystem;
public class ETLStorage
{
    public int GetLastPage(string downloaderName)
    {
        var resultsPath = NutrixPaths.GetDownloaderResult(downloaderName);
        if (!Directory.Exists(resultsPath))
        {
            _ = Directory.CreateDirectory(resultsPath);
        }

        var lastPage = Directory.GetFiles(resultsPath)
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
        var resultsPath = NutrixPaths.GetDownloaderResult(downloaderName);
        if (!Directory.Exists(resultsPath))
        {
            _ = Directory.CreateDirectory(resultsPath);
        }

        var fileName = $"{page}_{externalId}.html";
        File.WriteAllText(Path.Combine(resultsPath, fileName), content);
    }

    public IEnumerable<string> GetFilesToImport(string downloaderName) 
    {
        var resultsPath = NutrixPaths.GetDownloaderResult(downloaderName);
        if (!Directory.Exists(resultsPath))
        {
            _ = Directory.CreateDirectory(resultsPath);
        }

        return Directory.GetFiles(resultsPath).Where(x => Path.GetFileName(x) != "DownloadHistory.json");
    }
}
