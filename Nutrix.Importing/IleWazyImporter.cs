using HtmlAgilityPack;
using Nutrix.Commons.FileSystem;
using Nutrix.Database.Procedures;
using Nutrix.Downloader;

namespace Nutrix.Importing;
internal class IleWazyImporter
{
    private readonly HttpClient client = new();
    private readonly string resultsPath = NutrixPaths.GetDownloaderResult(nameof(IleWazyDownloader));

    public IleWazyImporter()
    {
        if (!Directory.Exists(resultsPath))
        {
            Directory.CreateDirectory(resultsPath);
        }
    }

    public async Task Import(string filename, string content)
    {
        var addOrUpdateProcedure = new AddOrUpdateProductProcedure();

        var html = new HtmlDocument();
        html.LoadHtml(content);
        var name = html.DocumentNode.SelectSingleNode("//h1[contains(@class, 'weighting-title')]").InnerText.Trim();
        var table = html.DocumentNode.SelectSingleNode("//table[@id='ilewazy-ingedients']");
        html.LoadHtml(table.InnerHtml);

        var values = table
        .SelectNodes("//tbody/tr")
        .Select(x =>
        {
            var text = x.ChildNodes[3].InnerText;
            var trimmed = text.Trim(" ,gkcal\r\n\\b.d.".ToCharArray());
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                trimmed = "0";
            }

            var per100g = decimal.Parse(trimmed);
            var per1000g = Convert.ToInt32(per100g * 10);
            return new
            {
                Name = x.ChildNodes[1].InnerText,
                Value = per1000g
            };

        }).ToArray();

        var externalId = filename.Split('_')[1].Split('.')[0];
        var product = new AddOrUpdateProductInput(
            "www.ilewazy.pl",
            externalId,
            name,
            values.First(x => x.Name.Contains("Energia")).Value,
            values.First(x => x.Name.Contains("Białko")).Value,
            values.First(x => x.Name.Contains("Tłuszcz")).Value,
            values.First(x => x.Name.Contains("Węglowodany")).Value,
            values.First(x => x.Name.Contains("Błonnik")).Value);

        await addOrUpdateProcedure.Execute(product);
        
    }
}
