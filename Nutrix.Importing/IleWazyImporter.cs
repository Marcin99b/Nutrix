using HtmlAgilityPack;
using Nutrix.Commons.ETL;
using Nutrix.Database.Procedures;

namespace Nutrix.Importing;
public class IleWazyImporter : IImporter
{
    public AddOrUpdateProductInput Import(ImportRequest request)
    {
        var html = new HtmlDocument();
        html.LoadHtml(request.Content);
        var name = html.DocumentNode.SelectSingleNode("//h1[contains(@class, 'weighting-title')]").InnerText.Trim();
        var table = html.DocumentNode.SelectSingleNode("//table[@id='ilewazy-ingedients']");
        html.LoadHtml(table.InnerHtml);

        var values = table
        .SelectNodes("//tbody/tr")
        .Select(x =>
        {
            var text = x.ChildNodes[3].InnerText;
            var trimmed = text.Trim(" ,gkcal\r\n\\b.d.".ToCharArray()).Replace(',', '.');
            var per100g = string.IsNullOrWhiteSpace(trimmed) ? 0 : decimal.Parse(trimmed);
            var per1000g = Convert.ToInt32(per100g * 10);
            return new
            {
                Name = x.ChildNodes[1].InnerText,
                Value = per1000g
            };

        }).ToArray();

        var product = new AddOrUpdateProductInput(
            request.Source,
            request.ExternalId,
            name,
            values.First(x => x.Name.Contains("Energia")).Value,
            values.First(x => x.Name.Contains("Białko")).Value,
            values.First(x => x.Name.Contains("Tłuszcz")).Value,
            values.First(x => x.Name.Contains("Węglowodany")).Value,
            values.First(x => x.Name.Contains("Błonnik")).Value);

        return product;
    }
}
