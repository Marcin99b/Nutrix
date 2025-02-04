using System.Reflection;

namespace Nutrix.Commons.FileSystem;
public class NutrixPaths(FileSystemProvider fileSystem)
{
    private readonly string assemblyPath = fileSystem.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

    public string GetDownloaderResult(string downloaderName)
        => fileSystem.Combine(this.assemblyPath, "DownloadResults", downloaderName);
}
