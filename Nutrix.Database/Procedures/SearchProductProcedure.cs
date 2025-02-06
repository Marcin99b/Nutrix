using Microsoft.EntityFrameworkCore;
using Nutrix.Database.Models;

namespace Nutrix.Database.Procedures;

public record SearchProductInput(string Query);
public record SearchProductOutput(IEnumerable<FoodProduct> Products);

public class SearchProductProcedure(IDbContextFactory<DatabaseContext> dbContextFactory)
{
    public async Task<SearchProductOutput> Execute(SearchProductInput input, CancellationToken ct)
    {
        using var ctx = await dbContextFactory.CreateDbContextAsync(ct);

        var products = await ctx.FoodProducts
            .Where(x => x.Name.ToLower().Contains(input.Query.ToLower()))
            .ToListAsync(ct);

        return new SearchProductOutput(products);
    }
}
