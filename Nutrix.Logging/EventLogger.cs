namespace Nutrix.Logging;
public class EventLogger(Serilog.ILogger logger)
{
    private const string LOG_TEMPLATE = "{Event} {Payload}";

    public void Downloader_Start(string downloaderName, int startingPage)
        => this.Info(nameof(Downloader_Start), new { DownloaderName = downloaderName, StartingPage = startingPage });

    private void Warn<T>(string eventName, T payload)
        => logger.Information(LOG_TEMPLATE, eventName, payload);

    private void Info<T>(string eventName, T payload)
        => logger.Information(LOG_TEMPLATE, eventName, payload);
}
