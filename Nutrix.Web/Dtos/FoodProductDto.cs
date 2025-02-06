namespace Nutrix.Web.Dtos;

public record FoodProductDto(
    int Id,
    string Name,
    decimal Kcal100g,
    decimal Proteins100g,
    decimal Fats100g,
    decimal Carbs100g,
    decimal Fiber100g);