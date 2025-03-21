﻿namespace Nutrix.Logging;
public class EventLogger(Serilog.ILogger logger)
{
    private const string LOG_TEMPLATE = "{Event} {@Payload}";

    public void Downloader_Started(string downloaderName)
        => this.Info(nameof(Downloader_Started), new { DownloaderName = downloaderName });

    public void Downloader_Finished(string downloaderName, int endingPage, bool isCancelled)
        => this.Info(nameof(Downloader_Finished), new { DownloaderName = downloaderName, EndingPage = endingPage, IsCancelled = isCancelled });

    public void Downloader_DownloadedPage(string downloaderName, int page, int productsOnPage, int productsDownloaded, int productsSaved)
        => this.Info(nameof(Downloader_DownloadedPage),
            new { DownloaderName = downloaderName, Page = page, ProductsOnPage = productsOnPage, ProductsDownloaded = productsDownloaded, ProductsSaved = productsSaved });

    public void Downloader_Exception(string downloaderName, int page, string url, Exception ex)
        => this.Err(nameof(Downloader_Exception), new { DownloaderName = downloaderName, Page = page, Url = url, Exception = ex });

    public void Importer_Exception(string importerName, string path, Exception ex)
        => this.Err(nameof(Importer_Exception), new { ImporterName = importerName, Path = path, Exception = ex });

    private void Warn<T>(string eventName, T payload)
        => logger.Warning(LOG_TEMPLATE, eventName, payload);

    private void Info<T>(string eventName, T payload)
        => logger.Information(LOG_TEMPLATE, eventName, payload);

    private void Err<T>(string eventName, T payload)
        => logger.Error(LOG_TEMPLATE, eventName, payload);
}
