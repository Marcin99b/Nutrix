using System.Reflection;

namespace Nutrix.Commons.FileSystem;
public static class NutrixPaths
{
    private static readonly string assemblyPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

    public static string GetDownloaderResult(string downloaderName)
        => Path.Combine(assemblyPath, "DownloadResults", downloaderName);
}
