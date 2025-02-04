using HtmlAgilityPack;
using Nutrix.Commons.FileSystem;
using Nutrix.Database.Procedures;
using Nutrix.Downloading;
using Nutrix.Logging;

namespace Nutrix.Importing;
public class IleWazyImporter(EventLogger eventLogger, ETLStorage storage)
{
    public async Task Import(CancellationToken ct)
    {
        var filesToImport = storage.GetFilesToImport(nameof(IleWazyDownloader)).ToArray();
        eventLogger.Importer_Started(nameof(IleWazyDownloader), filesToImport.Length);

        var addOrUpdateProcedure = new AddOrUpdateProductProcedure();
        var filesImported = 0;
        foreach (var path in filesToImport)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var fileName = Path.GetFileName(path);
                var content = File.ReadAllText(path);
                var product = this.ImportProduct(fileName, content);
                if (ct.IsCancellationRequested)
                {
                    break;
                }

                await addOrUpdateProcedure.Execute(product);
                File.Delete(path);
                filesImported++;
            }
            catch (Exception ex)
            {
                eventLogger.Importer_Exception(nameof(IleWazyDownloader), path, ex);
            }
        }

        eventLogger.Importer_Finished(nameof(IleWazyDownloader), filesToImport.Length, filesImported, ct.IsCancellationRequested);
    }

    private AddOrUpdateProductInput ImportProduct(string filename, string content)
    {
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

            trimmed = trimmed.Replace(',', '.');

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

        return product;
    }
}
