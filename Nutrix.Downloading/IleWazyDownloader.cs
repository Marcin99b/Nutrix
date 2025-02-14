using HtmlAgilityPack;
using Nutrix.Commons;
using Nutrix.Commons.ETL;
using Nutrix.Logging;
using System.Threading.Channels;

namespace Nutrix.Downloading;
public class IleWazyDownloader(EventLogger eventLogger, DownloadHistoryFactory downloadHistoryFactory, Channel<ImportRequest> channel) : IDownloader
{
    private readonly int delayMs = 200;
    private readonly HttpClient client = new();

    public async Task Download(CancellationToken ct)
    {
        eventLogger.Downloader_Started(nameof(IleWazyDownloader));

        var history = downloadHistoryFactory.CreateOrLoad(nameof(IleWazyDownloader));

        var morePages = true;
        var lastPage = 1;
        while (morePages && !ct.IsCancellationRequested)
        {
            morePages = await this.DownloadPage(lastPage, history, ct);
            history.Save(nameof(IleWazyDownloader));
            if (morePages && !ct.IsCancellationRequested)
            {
                lastPage++;
            }
        }

        eventLogger.Downloader_Finished(nameof(IleWazyDownloader), lastPage - (ct.IsCancellationRequested ? 0 : 1), ct.IsCancellationRequested);
    }

    private async Task<bool> DownloadPage(int page, DownloadHistory history, CancellationToken ct)
    {
        var productsOnPage = await this.GetUrlsToProductsOnPage(page);
        if (productsOnPage.Length == 0)
        {
            return false;
        }

        await Task.Delay(this.delayMs);

        var productsDownloaded = 0;
        var productsSaved = 0;
        foreach (var productUrl in productsOnPage)
        {
            try
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                var (isDownloaded, isSaved) = await this.TrySaveProduct(history, productUrl);
                if (isDownloaded)
                {
                    productsDownloaded++;
                    if (isSaved)
                    {
                        productsSaved++;
                    }

                    await Task.Delay(this.delayMs);
                }
            }
            catch (Exception ex)
            {
                eventLogger.Downloader_Exception(nameof(IleWazyDownloader), page, productUrl, ex);
            }
        }

        eventLogger.Downloader_DownloadedPage(nameof(IleWazyDownloader), page, productsOnPage.Length, productsDownloaded, productsSaved);

        return true;
    }

    private async Task<string[]> GetUrlsToProductsOnPage(int page)
    {
        var url = this.GetPageUrl(page);

        var content = await this.client!.GetStringAsync(url);
        if (content.Contains("Niestety nie udało nam się nic dla Ciebie znaleźć..."))
        {
            return [];
        }

        var html = new HtmlDocument();
        html!.LoadHtml(content);
        var items = html.DocumentNode.SelectNodes("//li[contains(@class, 'span3')]/div[contains(@class, 'subtitle')]/a")
            .Select(x => x.Attributes.First(x => x.Name == "href").Value)
            .Select(x => x.RemoveDiacritics())
            .ToArray();

        return items;
    }

    private string GetPageUrl(int id) => $"http://www.ilewazy.pl/produkty/page/{id}/s/date/k/asc/".RemoveDiacritics();

    private async Task<(bool Downloaded, bool Saved)> TrySaveProduct(DownloadHistory history, string productUrl)
    {
        var externalId = productUrl.Replace("http://www.ilewazy.pl/", string.Empty);
        var historyItem = history.Get(externalId);
        if (historyItem?.ShouldTryDownload() == false)
        {
            //skip if last change was too recently
            return (false, false);
        }

        var content = CutContent(await this.client!.GetStringAsync(productUrl));
        var hash = content.HashMD5();

        if (historyItem == null)
        {
            history.Items.Add(new DownloadHistoryItem(externalId, hash));
        }
        else if (historyItem.Hash == hash)
        {
            // skip if data don't changed
            historyItem.LastDownloadAttempt = DateTime.Now;
            return (true, false);
        }
        else
        {
            historyItem.UpdateHash(hash);
        }

        await channel.Writer.WriteAsync(new ImportRequest(DownloaderSources.IleWazy, externalId, content));
        return (true, true);
    }

    private static string CutContent(string input)
    {
        var itemHtml = new HtmlDocument();
        itemHtml.LoadHtml(input);
        var interestingArea = itemHtml.DocumentNode
            .SelectNodes("//div[contains(@class, 'container main')]/div[contains(@class, 'row')]")
            .Select(x => x.InnerHtml)
            .Aggregate((a, b) => $"{a}\r\n{b}");

        var adsIndex = interestingArea!.IndexOf(@"<!--podobne produkty-->");
        return interestingArea[..adsIndex];
    }
}