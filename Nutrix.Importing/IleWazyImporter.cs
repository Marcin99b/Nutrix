using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nutrix.Importing;
internal class IleWazyImporter
{
    public void Import()
    {

        var resultsPath = @"C:/Users/marci/Desktop/Nutrix/Downloader/Nutrix.Downloader/Results";
        string[] files = Directory.GetFiles(resultsPath);

        var products = new ConcurrentBag<Product>();
        Parallel.ForEach(files, file => Process(File.ReadAllText(file)));
        await InsertToDatabase();

        void Process(string content)
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

            var product = new Product(
                name,
                values.First(x => x.Name.Contains("Energia")).Value,
                values.First(x => x.Name.Contains("Białko")).Value,
                values.First(x => x.Name.Contains("Tłuszcz")).Value,
                values.First(x => x.Name.Contains("Węglowodany")).Value,
                values.First(x => x.Name.Contains("Błonnik")).Value);

            products!.Add(product);
        }
    }

    async Task InsertToDatabase()
    {
        var connectionString = "Host=localhost;Username=postgres;Database=postgres";
        await using var dataSource = NpgsqlDataSource.Create(connectionString);

        foreach (var product in products)
        {
            await using (var cmd = dataSource.CreateCommand(
                @"INSERT INTO food_products (name, calories_1000g, proteins_1000g, fats_1000g, carbs_1000g, fiber_1000g)
        VALUES (($1), ($2), ($3), ($4), ($5), ($6))"))
            {
                cmd.Parameters.AddWithValue(product.Title);
                cmd.Parameters.AddWithValue(product.KcalPer1000g);
                cmd.Parameters.AddWithValue(product.ProteinPer1000g);
                cmd.Parameters.AddWithValue(product.FatPer1000g);
                cmd.Parameters.AddWithValue(product.CarbsPer1000g);
                cmd.Parameters.AddWithValue(product.FiberPer1000g);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        products.Clear();
    }

    record Product(string Title, int KcalPer1000g, int ProteinPer1000g, int FatPer1000g, int CarbsPer1000g, int FiberPer1000g);
}
