
namespace Nutrix.Downloading;

public interface IDownloader
{
    Task Download(CancellationToken ct);
}