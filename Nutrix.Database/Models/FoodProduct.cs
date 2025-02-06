using System.ComponentModel.DataAnnotations;

namespace Nutrix.Database.Models;

public class FoodProduct
{
    [Key]
    public int Id { get; set; }
    public string Source { get; set; }
    public string ExternalId { get; set; }
    public string Name { get; set; }
    public decimal Kcal1000g { get; set; }
    public decimal Proteins1000g { get; set; }
    public decimal Fats1000g { get; set; }
    public decimal Carbs1000g { get; set; }
    public decimal Fiber1000g { get; set; }
}
