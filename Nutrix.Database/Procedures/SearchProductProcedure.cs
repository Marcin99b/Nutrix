using Microsoft.EntityFrameworkCore;
using Nutrix.Database.Models;

namespace Nutrix.Database.Procedures;

public record SearchProductInput(string Query);
public record SearchProductOutput(IEnumerable<FoodProduct> Products);

public class SearchProductProcedure(IDbContextFactory<DatabaseContext> dbContextFactory)
{
    public async Task<SearchProductOutput> Execute(SearchProductInput input)
    {
        using var ctx = await dbContextFactory.CreateDbContextAsync();

        var products = await ctx.FoodProducts
            .Where(x => x.Name.Contains(input.Query, StringComparison.InvariantCultureIgnoreCase))
            .ToListAsync();

        return new SearchProductOutput(products);
    }
}
