using Newtonsoft.Json;
using Nutrix.Commons.FileSystem;

namespace Nutrix.Commons.ETL;
public record DownloadHistory
{
    public List<DownloadHistoryItem> Items { get; } = [];
    public DateTime LastDownload { get; private set; } = default;

    private DownloadHistory()
    {
    }

    public static DownloadHistory CreateOrLoad(string downloaderName)
    {
        var path = Path.Combine(NutrixPaths.GetDownloaderResult(downloaderName), "DownloadHistory.json");
        if (!File.Exists(path))
        {
            return new DownloadHistory();
        }

        var content = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<DownloadHistory>(content);
    }

    public void Save(string downloaderName)
    {
        this.LastDownload = DateTime.Now;
        var path = Path.Combine(NutrixPaths.GetDownloaderResult(downloaderName), "DownloadHistory.json");
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}

public record DownloadHistoryItem(string ExternalId, string Hash)
{
    public string Hash { get; private set; } = Hash;
    public DateTime FirstDownload { get; } = DateTime.Now;
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
        //time is 2x more than time between last attempt and last found change
        //check changes
        if (this.TotalModifications == 0)
        {
            return (DateTime.Now - this.LastDownloadAttempt) * 2 > this.LastDownloadAttempt - this.FirstDownload;
        }

        //if download is 1 time per day
        //average content change is 1 time per week
        //and there are more than week from last attempt
        //check changes
        var avg = this.GetAverageModificationTimespan();
        var timeToLast = DateTime.Now - this.LastDownloadAttempt;
        return timeToLast >= avg;
    }
}