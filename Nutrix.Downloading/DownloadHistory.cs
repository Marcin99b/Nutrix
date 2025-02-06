using Newtonsoft.Json;
using Nutrix.Commons.FileSystem;

namespace Nutrix.Downloading;

public class DownloadHistoryFactory(NutrixPaths nutrixPaths, FileSystemProvider fileSystem)
{
    public DownloadHistory CreateOrLoad(string downloaderName)
    {
        var path = fileSystem.Combine(nutrixPaths.GetDownloaderResult(downloaderName), "DownloadHistory.json");
        if (!fileSystem.Exists(path))
        {
            return new DownloadHistory().Setup(nutrixPaths, fileSystem);
        }

        var content = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<DownloadHistory>(content)!.Setup(nutrixPaths, fileSystem);
    }
}

public class DownloadHistory
{
    private NutrixPaths? nutrixPaths;
    private FileSystemProvider? fileSystem;

    public List<DownloadHistoryItem> Items { get; } = [];
    public DateTime LastDownload { get; private set; } = default;

    public DownloadHistoryItem? Get(string externalId)
        => this.Items.FirstOrDefault(x => x.ExternalId == externalId);

    internal DownloadHistory Setup(NutrixPaths nutrixPaths, FileSystemProvider fileSystem)
    {
        this.nutrixPaths = nutrixPaths;
        this.fileSystem = fileSystem;
        return this;
    }

    public void Save(string downloaderName)
    {
        this.LastDownload = DateTime.Now;
        var path = this.fileSystem!.Combine(this.nutrixPaths!.GetDownloaderResult(downloaderName), "DownloadHistory.json");
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        this.fileSystem!.WriteAllText(path, json);
    }
}

public record DownloadHistoryItem(string ExternalId, string Hash)
{
    public string Hash { get; private set; } = Hash;
    public DateTime FirstDownload { get; private set; } = DateTime.Now;
    public DateTime LastFoundModification { get; private set; } = default;
    public DateTime LastDownloadAttempt { get; set; } = DateTime.Now;
    public int TotalModifications { get; private set; } = 0;

    public void UpdateHash(string newHash)
    {
        this.LastFoundModification = DateTime.Now;
        this.LastDownloadAttempt = DateTime.Now;
        this.TotalModifications++;
        this.Hash = newHash;
    }

    public TimeSpan GetAverageModificationTimespan()
    {
        return this.TotalModifications == 0
            ? DateTime.Now - this.FirstDownload
            : (this.LastFoundModification - this.FirstDownload) / this.TotalModifications;
    }

    public bool ShouldTryDownload()
    {
        //if content never changed
        //time is 2x more than time between last attempt and first download
        //check changes
        if (this.TotalModifications == 0)
        {
            return (DateTime.Now - this.LastDownloadAttempt) * 2 > this.LastDownloadAttempt - this.FirstDownload;
        }

        //if download is 1 time per day
        //average content change is 1 time per week
        //and it's been over a week since the last attempt
        //check changes
        var avg = this.GetAverageModificationTimespan();
        var timeToLast = DateTime.Now - this.LastDownloadAttempt;
        return timeToLast >= avg;
    }
}