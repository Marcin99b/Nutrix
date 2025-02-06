using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Nutrix.Database.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.CreateTable(
            name: "FoodProducts",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Source = table.Column<string>(type: "text", nullable: false),
                ExternalId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Kcal1000g = table.Column<int>(type: "integer", nullable: false),
                Proteins1000g = table.Column<int>(type: "integer", nullable: false),
                Fats1000g = table.Column<int>(type: "integer", nullable: false),
                Carbs1000g = table.Column<int>(type: "integer", nullable: false),
                Fiber1000g = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_FoodProducts", x => x.Id));
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "FoodProducts");
    }
}
