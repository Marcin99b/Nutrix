using HtmlAgilityPack;
using Nutrix.Commons.FileSystem;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Nutrix.Downloader;
internal class IleWazyDownloader
{
    private readonly HttpClient client = new();
    private readonly string resultsPath = NutrixPaths.GetDownloaderResult(nameof(IleWazyDownloader));

    public IleWazyDownloader()
    {
        if (!Directory.Exists(resultsPath))
        {
            Directory.CreateDirectory(resultsPath);
        }
    }

    public async Task Download()
    {
        var lastPage = Directory.GetFiles(resultsPath)
            .Select(Path.GetFileName)
            .Select(x => x!.Split('_')[0])
            .Select(int.Parse)
            .OrderByDescending(x => x)
            .FirstOrDefault(1);

        var morePages = false;
        do
        {
            morePages = await this.DownloadPage(lastPage);
            if (morePages)
            {
                lastPage++;
            }
        } while (morePages);
    }

    async Task<bool> DownloadPage(int page)
    {
        var productsOnPage = await this.GetUrlsToProductsOnPage(page);
        if (productsOnPage.Length == 0)
        {
            return false;
        }

        foreach (var productUrl in productsOnPage)
        {
            await Task.Delay(200);
            var name = productUrl.Replace("http://www.ilewazy.pl/", string.Empty);

            var sw = Stopwatch.StartNew();
            var content = await client!.GetStringAsync(productUrl);
            sw.Stop();

            var fileName = $"{page}_{name}.html";
            File.WriteAllText(Path.Combine(resultsPath!, fileName), content);
        }

        var totalProducts = Directory.GetFiles(resultsPath).Length;
        return totalProducts > 0;
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
}