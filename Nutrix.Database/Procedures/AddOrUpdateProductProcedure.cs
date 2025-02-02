using Npgsql;

namespace Nutrix.Database.Procedures;

public record AddOrUpdateProductInput(
    string Source,
    string ExternalId,
    string Name,
    int Kcal1000g,
    int Proteins1000g,
    int Fats1000g,
    int Carbs1000g,
    int Fiber1000g);

public class AddOrUpdateProductProcedure
{
    public async Task Execute(AddOrUpdateProductInput input)
    {
        var connectionString = "Host=localhost;Username=postgres;Database=postgres";
        await using var dataSource = NpgsqlDataSource.Create(connectionString);

        var query = $@"
        DO $$
        BEGIN
            IF EXISTS (SELECT FROM public.food_products WHERE source = '{input.Source.Replace("'", "''")}' AND external_id = '{input.ExternalId.Replace("'", "''")}') THEN
                UPDATE public.food_products
                SET name = '{input.Name.Replace("'", "''")}', kcal_1000g = {input.Kcal1000g}, proteins_1000g = {input.Proteins1000g}, fats_1000g = {input.Fats1000g}, carbs_1000g = {input.Carbs1000g}, fiber_1000g = {input.Fiber1000g}
                WHERE source = '{input.Source.Replace("'", "''")}' AND external_id = '{input.ExternalId.Replace("'", "''")}';
            ELSE
                INSERT INTO public.food_products
                (source, external_id, name, kcal_1000g, proteins_1000g, fats_1000g, carbs_1000g, fiber_1000g)
                VALUES 
                ('{input.Source.Replace("'", "''")}', '{input.ExternalId.Replace("'", "''")}', '{input.Name.Replace("'", "''")}', {input.Kcal1000g}, {input.Proteins1000g}, {input.Fats1000g}, {input.Carbs1000g}, {input.Fiber1000g});
            END IF;
        END;
        $$";

        await using var cmd = dataSource.CreateCommand(query);
        await cmd.ExecuteNonQueryAsync();
    }
}
