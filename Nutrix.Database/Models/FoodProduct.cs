namespace Nutrix.Database.Models;

public record FoodProduct(
    int Id,
    string Source,
    string ExternalId,
    string Name,
    int Kcal1000g,
    int Proteins1000g,
    int Fats1000g,
    int Carbs1000g,
    int Fiber1000g);