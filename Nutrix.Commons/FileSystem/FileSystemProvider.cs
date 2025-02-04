

namespace Nutrix.Commons.FileSystem;
public class FileSystemProvider
{
    public string Combine(params string[] paths) => Path.Combine(paths);
    public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);
    public void WriteAllText(string path, string? contents) => File.WriteAllText(path, contents);
    public bool Exists(string path) => Path.Exists(path);
    public string GetFileName(string path) => Path.GetFileName(path);
    public IEnumerable<string> GetFiles(string path) => Directory.GetFiles(path);
    public DirectoryInfo CreateDirectory(string path) => Directory.CreateDirectory(path);
}
