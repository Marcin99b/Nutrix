using HtmlAgilityPack;
using Nutrix.Commons.ETL;
using Nutrix.Commons.FileSystem;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;

namespace Nutrix.Downloader;
public class IleWazyDownloader
{
    private readonly HttpClient client = new();
    private readonly string resultsPath = NutrixPaths.GetDownloaderResult(nameof(IleWazyDownloader));
    private readonly int delayMs;

    public IleWazyDownloader(int delayMs)
    {
        if (!Directory.Exists(resultsPath))
        {
            Directory.CreateDirectory(resultsPath);
        }

        this.delayMs = delayMs;
    }

    public async Task Download()
    {
        var history = DownloadHistory.CreateOrLoad(nameof(IleWazyDownloader));

        var lastPage = Directory.GetFiles(resultsPath)
            .Select(Path.GetFileName)
            .Where(x => x != "DownloadHistory.json")
            .Select(x => x!.Split('_')[0])
            .Select(int.Parse)
            .OrderByDescending(x => x)
            .FirstOrDefault(1);

        var morePages = false;
        do
        {
            morePages = await this.DownloadPage(lastPage, history);
            if (morePages)
            {
                history.Save(nameof(IleWazyDownloader));
                lastPage++;
            }
        } while (morePages);

        history.Save(nameof(IleWazyDownloader));
    }

    async Task<bool> DownloadPage(int page, DownloadHistory history)
    {
        var productsOnPage = await this.GetUrlsToProductsOnPage(page);
        if (productsOnPage.Length == 0)
        {
            return false;
        }

        await Task.Delay(delayMs);

        foreach (var productUrl in productsOnPage)
        {
            var isDownloadedFromServer = await this.TrySaveProduct(history, page, productUrl);
            if (isDownloadedFromServer)
            {
                await Task.Delay(delayMs);
            }
        }

        return true;
    }

    async Task<string[]> GetUrlsToProductsOnPage(int page)
    {
        var url = this.GetPageUrl(page);

        var content = await client!.GetStringAsync(url);
        if (content.Contains("Niestety nie udało nam się nic dla Ciebie znaleźć..."))
        {
            return Array.Empty<string>();
        }

        var html = new HtmlDocument();
        html!.LoadHtml(content);
        var items = html.DocumentNode.SelectNodes("//li[contains(@class, 'span3')]/div[contains(@class, 'subtitle')]/a")
            .Select(x => x.Attributes.First(x => x.Name == "href").Value)
            .Select(this.RemoveDiacritics)
            .ToArray();

        return items;
    }

    string GetPageUrl(int id) => this.RemoveDiacritics($"http://www.ilewazy.pl/produkty/page/{id}/s/date/k/asc/");

    private async Task<bool> TrySaveProduct(DownloadHistory history, int page, string productUrl)
    {
        var name = productUrl.Replace("http://www.ilewazy.pl/", string.Empty);

        var foundItem = history.Items.FirstOrDefault(x => x.ExternalId == name);
        if (foundItem?.ShouldTryDownload() == false)
        {
            //skip if last change was too recently
            return false;
        }

        var content = await this.DownloadProduct(productUrl);
        content = this.CutContent(content);
        var hash = this.HashMD5(content);

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
        File.WriteAllText(Path.Combine(resultsPath!, fileName), content);
        return true;
    }

    private string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (char c in normalizedString)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private string HashMD5(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes);
    }

    private string CutContent(string input)
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

    private async Task<string> DownloadProduct(string url)
    {
        var sw = Stopwatch.StartNew();
        var content = await client!.GetStringAsync(url);
        sw.Stop();
        //todo log performance
        return content;
    }
}