using Nutrix.Downloading;

public class ETLManager(IServiceProvider serviceProvider)
{
    public async Task RunDownloader(string source, CancellationToken ct) 
    {
        var obj = serviceProvider.GetRequiredKeyedService<IDownloader>(source);
        await obj.Download(ct);
    }
}