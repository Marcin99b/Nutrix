using Npgsql;

namespace Nutrix.Database.Procedures;

public record AddOrUpdateProductInput(
    string Source,
    string ExternalId,
    string Name,
    int Kcal1000g,
    int Protein1000g,
    int Fat1000g,
    int Carbs1000g,
    int Fiber1000g);

public class AddOrUpdateProductProcedure
{
    public async Task Execute(AddOrUpdateProductInput input)
    {
        var connectionString = "Host=localhost;Username=postgres;Database=postgres";
        await using var dataSource = NpgsqlDataSource.Create(connectionString);

        await using var cmd = dataSource.CreateCommand(
            @"INSERT INTO food_products
            (source, external_id, name, kcal_1000g, proteins_1000g, fats_1000g, carbs_1000g, fiber_1000g)
            VALUES 
            (($1), ($2), ($3), ($4), ($5), ($6), ($7), ($8))");
        
        cmd.Parameters.AddWithValue(input.Source);
        cmd.Parameters.AddWithValue(input.ExternalId);
        cmd.Parameters.AddWithValue(input.Name);
        cmd.Parameters.AddWithValue(input.Kcal1000g);
        cmd.Parameters.AddWithValue(input.Protein1000g);
        cmd.Parameters.AddWithValue(input.Fat1000g);
        cmd.Parameters.AddWithValue(input.Carbs1000g);
        cmd.Parameters.AddWithValue(input.Fiber1000g);

        await cmd.ExecuteNonQueryAsync();
        
    }
}
