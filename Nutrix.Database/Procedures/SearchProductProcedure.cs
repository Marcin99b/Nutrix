using Npgsql;
using Nutrix.Database.Models;

namespace Nutrix.Database.Procedures;

public record SearchProductInput(string Query);
public record SearchProductOutput(IEnumerable<FoodProduct> Products);

public class SearchProductProcedure
{
    public async Task<SearchProductOutput> Execute(SearchProductInput input)
    {
        var connectionString = "Host=localhost;Username=postgres;Database=postgres";
        await using var dataSource = NpgsqlDataSource.Create(connectionString);

        var query = @"  SELECT id, ""source"", external_id, ""name"", kcal_1000g, proteins_1000g, fats_1000g, carbs_1000g, fiber_1000g 
                        FROM public.food_products
                        WHERE ""name"" ILIKE '%' || @query || '%';";

        await using var cmd = dataSource.CreateCommand(query);
        cmd.Parameters.AddWithValue("query", input.Query);

        using var reader = await cmd.ExecuteReaderAsync();
        var items = new List<FoodProduct>();
        while (await reader.ReadAsync())
        {
            //var item = new FoodProduct(
            //    reader.GetInt32(reader.GetOrdinal("id")),
            //    reader.GetString(reader.GetOrdinal("source")),
            //    reader.GetString(reader.GetOrdinal("external_id")),
            //    reader.GetString(reader.GetOrdinal("name")),
            //    reader.GetInt32(reader.GetOrdinal("kcal_1000g")),
            //    reader.GetInt32(reader.GetOrdinal("proteins_1000g")),
            //    reader.GetInt32(reader.GetOrdinal("fats_1000g")),
            //    reader.GetInt32(reader.GetOrdinal("carbs_1000g")),
            //    reader.GetInt32(reader.GetOrdinal("fiber_1000g"))
            //    );
            //items.Add(item);
        }

        return new SearchProductOutput(items);
    }
}
