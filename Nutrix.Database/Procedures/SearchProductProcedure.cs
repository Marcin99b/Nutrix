﻿using Microsoft.EntityFrameworkCore;
using Npgsql;
using Nutrix.Database.Models;

namespace Nutrix.Database.Procedures;

public record SearchProductInput(string Query);
public record SearchProductOutput(IEnumerable<FoodProduct> Products);

public class SearchProductProcedure
{
    public async Task<SearchProductOutput> Execute(SearchProductInput input)
    {
        using var ctx = new DatabaseContext();

        var products = await ctx.FoodProducts
            .Where(x => x.Name.Contains(input.Query))
            .ToListAsync();

        return new SearchProductOutput(products);
    }
}
