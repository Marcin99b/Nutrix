using Microsoft.EntityFrameworkCore;
using Nutrix.Database.Models;

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

public class AddOrUpdateProductProcedure(IDbContextFactory<DatabaseContext> dbContextFactory)
{
    public async Task Execute(IEnumerable<AddOrUpdateProductInput> inputs, CancellationToken ct)
    {
        using var ctx = await dbContextFactory.CreateDbContextAsync(ct);

        var products = inputs.Select(input => new FoodProduct()
        {
            Source = input.Source,
            ExternalId = input.ExternalId,
            Name = input.Name,
            Kcal1000g = input.Kcal1000g,
            Proteins1000g = input.Proteins1000g,
            Fats1000g = input.Fats1000g,
            Carbs1000g = input.Carbs1000g,
            Fiber1000g = input.Fiber1000g
        });

        foreach (var product in products)
        {
            var found = await ctx.FoodProducts.FirstOrDefaultAsync(x => x.Source == product.Source && x.ExternalId == product.ExternalId);
            if (found == null)
            {
                _ = await ctx.FoodProducts.AddAsync(product, ct);
            }
            else
            {
                product.Id = found.Id;
                _ = ctx.FoodProducts.Update(product);
            }
        }

        _ = await ctx.SaveChangesAsync(ct);
    }
}
