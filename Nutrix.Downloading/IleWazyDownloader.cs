using HtmlAgilityPack;
using Nutrix.Commons;
using Nutrix.Commons.ETL;
using Nutrix.Commons.FileSystem;
using Nutrix.Logging;

namespace Nutrix.Downloading;
public class IleWazyDownloader
{
    private readonly HttpClient client = new();
    private readonly string resultsPath = NutrixPaths.GetDownloaderResult(nameof(IleWazyDownloader));
    private readonly int delayMs;
    private readonly EventLogger eventLogger;

    public IleWazyDownloader(int delayMs, EventLogger eventLogger)
    {
        if (!Directory.Exists(this.resultsPath))
        {
            _ = Directory.CreateDirectory(this.resultsPath);
        }

        this.delayMs = delayMs;
        this.eventLogger = eventLogger;
    }

    public async Task Download()
    {
        var history = DownloadHistory.CreateOrLoad(nameof(IleWazyDownloader));

        var lastPage = Directory.GetFiles(this.resultsPath)
            .Select(Path.GetFileName)
            .Where(x => x != "DownloadHistory.json")
            .Select(x => x!.Split('_')[0])
            .Select(int.Parse)
            .OrderByDescending(x => x)
            .FirstOrDefault(1);

        this.eventLogger.Downloader_Start(nameof(IleWazyDownloader), lastPage);

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
    }

    private async Task<bool> DownloadPage(int page, DownloadHistory history)
    {
        var productsOnPage = await this.GetUrlsToProductsOnPage(page);
        if (productsOnPage.Length == 0)
        {
            return false;
        }

        await Task.Delay(this.delayMs);

        foreach (var productUrl in productsOnPage)
        {
            var isDownloadedFromServer = await this.TrySaveProduct(history, page, productUrl);
            if (isDownloadedFromServer)
            {
                await Task.Delay(this.delayMs);
            }
        }

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

    private async Task<bool> TrySaveProduct(DownloadHistory history, int page, string productUrl)
    {
        var name = productUrl.Replace("http://www.ilewazy.pl/", string.Empty);

        var foundItem = history.Items.FirstOrDefault(x => x.ExternalId == name);
        if (foundItem?.ShouldTryDownload() == false)
        {
            //skip if last change was too recently
            return false;
        }

        var content = CutContent(await this.client!.GetStringAsync(productUrl));
        var hash = content.HashMD5();

        if (foundItem == null)
        {
            history.Items.Add(new DownloadHistoryItem(name, hash));
        }
        else if (foundItem.Hash == hash)
        {
            // skip if data don't changed
            foundItem.LastDownloadAttempt = DateTime.Now;
            return true;
        }
        else
        {
            foundItem.UpdateHash(hash);
        }

        var fileName = $"{page}_{name}.html";
        File.WriteAllText(Path.Combine(this.resultsPath!, fileName), content);
        return true;
    }

    private static string CutContent(string input)
    {
        var itemHtml = new HtmlDocument();
        itemHtml.LoadHtml(input);
        var interestingArea = itemHtml.DocumentNode
            .SelectNodes("//div[contains(@class, 'container main')]/div[contains(@class, 'row')]")
            .Select(x => x.InnerHtml)
            .ToArray()
            .Aggregate((a, b) => $"{a}\r\n{b}");

        var adsIndex = interestingArea!.IndexOf(@"<!--podobne produkty-->");
        return interestingArea[..adsIndex];
    }
}