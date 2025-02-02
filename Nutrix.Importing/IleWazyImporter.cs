using HtmlAgilityPack;
using Nutrix.Database.Procedures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nutrix.Importing;
internal class IleWazyImporter
{
    public async Task Import(string filename, string content)
    {
        string[] files = Directory.GetFiles(resultsPath);
        var addOrUpdateProcedure = new AddOrUpdateProductProcedure();

        foreach (var file in files)
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
                try
                {
                    var text = x.ChildNodes[3].InnerText;
                    var trimmed = text.Trim(" ,gkcal\r\n\\b.d.".ToCharArray());
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        trimmed = "0";
                    }

                    var dec100g = decimal.Parse(trimmed);
                    var per1000g = Convert.ToInt32(dec100g * 10);
                    return new
                    {
                        Name = x.ChildNodes[1].InnerText,
                        Value = per1000g
                    };
                }
                catch (Exception ex)
                {
                    throw;
                }

            }).ToArray();

            var product = new AddOrUpdateProductInput(
                "www.ilewazy.pl",
                "TODO",
                name,
                values.First(x => x.Name.Contains("Energia")).Value,
                values.First(x => x.Name.Contains("Białko")).Value,
                values.First(x => x.Name.Contains("Tłuszcz")).Value,
                values.First(x => x.Name.Contains("Węglowodany")).Value,
                values.First(x => x.Name.Contains("Błonnik")).Value);

            await addOrUpdateProcedure.Execute(product);
        }
    }
}
