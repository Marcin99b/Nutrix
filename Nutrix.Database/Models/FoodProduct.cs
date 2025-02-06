using System.ComponentModel.DataAnnotations;

namespace Nutrix.Database.Models;

public class FoodProduct
{
    [Key]
    public int Id { get; set; }
    public required string Source { get; set; }
    public required string ExternalId { get; set; }
    public required string Name { get; set; }
    public int Kcal1000g { get; set; }
    public int Proteins1000g { get; set; }
    public int Fats1000g { get; set; }
    public int Carbs1000g { get; set; }
    public int Fiber1000g { get; set; }
}
