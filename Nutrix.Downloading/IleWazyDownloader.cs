using HtmlAgilityPack;
using Nutrix.Commons;
using Nutrix.Commons.FileSystem;
using Nutrix.Logging;

namespace Nutrix.Downloading;
public class IleWazyDownloader(EventLogger eventLogger, ETLStorage storage, DownloadHistoryFactory downloadHistoryFactory)
{
    private readonly int delayMs = 200;
    private readonly HttpClient client = new();

    public async Task Download()
    {
        var history = downloadHistoryFactory.CreateOrLoad(nameof(IleWazyDownloader));
        var lastPage = storage.GetLastPage(nameof(IleWazyDownloader));

        eventLogger.Downloader_Started(nameof(IleWazyDownloader), lastPage);

        var morePages = true;
        while (morePages)
        {
            morePages = await this.DownloadPage(lastPage, history);
            history.Save(nameof(IleWazyDownloader));
            if (morePages)
            {
                lastPage++;
            }
        }

        eventLogger.Downloader_Finished(nameof(IleWazyDownloader), lastPage - 1);
    }

    private async Task<bool> DownloadPage(int page, DownloadHistory history)
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
            var (isDownloaded, isSaved) = await this.TrySaveProduct(history, page, productUrl);
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

    private async Task<(bool Downloaded, bool Saved)> TrySaveProduct(DownloadHistory history, int page, string productUrl)
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

        storage.Save(nameof(IleWazyDownloader), page, externalId, content);
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