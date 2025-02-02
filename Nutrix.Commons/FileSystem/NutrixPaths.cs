namespace Nutrix.Commons.FileSystem;
public static class NutrixPaths
{
    public static string GetDownloaderResult(string downloaderName)
        => Path.Combine(Directory.GetCurrentDirectory(), "DownloadResults", downloaderName);
}
