using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nutrix.Database.Models;
using System;

namespace Nutrix.Database;

public class DatabaseContext : DbContext
{
    public DbSet<FoodProduct> FoodProducts { get; set; }

    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public DatabaseContext()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql($"Host=localhost;Username=postgres;Database=postgres");
}
