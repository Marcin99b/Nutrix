using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nutrix.Downloader;
internal class IleWazyDownloader
{
    public void Download()
    {

        var resultsPath = "C:/Users/marci/Desktop/Nutrix/Downloader/Nutrix.Downloader/Results";
        var client = new HttpClient();
        var html = new HtmlDocument();

        var lastPage = Directory.GetFiles(resultsPath)
            .Select(Path.GetFileName)
            .Select(x => x!.Split('_')[0])
            .Select(int.Parse)
            .OrderByDescending(x => x)
            .FirstOrDefault(1);

        await DownloadPage(lastPage);

        async Task DownloadPage(int page)
        {
            var productsOnPage = await GetUrlsToProductsOnPage(page);
            if (productsOnPage.Length == 0)
            {
                return;
            }

            foreach (var productUrl in productsOnPage)
            {
                Log($"Waiting 200ms before download product");
                await Task.Delay(200);
                var name = productUrl.Replace("http://www.ilewazy.pl/", string.Empty);
                var sw = Stopwatch.StartNew();
                var content = await DownloadString(productUrl);
                sw.Stop();
                Log($"Downloaded product {name} in time {sw.ElapsedMilliseconds}ms");
                var fileName = $"{page}_{name}.html";
                File.WriteAllText(Path.Combine(resultsPath!, fileName), content);
                Log($"Saved file {fileName} with length {content.Length}");
            }

            var totalProducts = Directory.GetFiles(resultsPath).Length;
            Log($"Total downloaded products: {totalProducts}");
            Log("===========================================");
            await DownloadPage(page + 1);
        }
    }
}


async Task<string[]> GetUrlsToProductsOnPage(int page)
{
    var url = GetPageUrl(page);

    var sw = Stopwatch.StartNew();
    var content = await DownloadString(url);

    if (content.Contains("Niestety nie udało nam się nic dla Ciebie znaleźć..."))
    {
        Log($"Page {page} not found. Last existing is {page - 1}");
        return Array.Empty<string>();
    }

    sw.Stop();
    Log($"Downloaded page {page} in time {sw.ElapsedMilliseconds}ms");

    html!.LoadHtml(content);

    var items = html.DocumentNode.SelectNodes("//li[contains(@class, 'span3')]/div[contains(@class, 'subtitle')]/a")
        .Select(x => x.Attributes.First(x => x.Name == "href").Value)
        .ToArray();

    Log($"Found {items.Length} products on page {page}");
    return items;
}

string GetPageUrl(int id) => $"http://www.ilewazy.pl/produkty/page/{id}/s/date/k/asc/";

void Log(string text) => Console.WriteLine($"{DateTime.Now:HH-mm-ss}: {text}");

async Task<string> DownloadString(string url)
{
    var normalizedUrl = RemoveDiacritics(url);
    return await client!.GetStringAsync(normalizedUrl);
}

string RemoveDiacritics(string text)
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
            stringBuilder.Append(c);
    }

    return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
}