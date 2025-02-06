using Nutrix.Database.Models;

namespace Nutrix.Web.Dtos;

public record FoodProductDto(
    int Id,
    string Name,
    decimal Kcal100g,
    decimal Proteins100g,
    decimal Fats100g,
    decimal Carbs100g,
    decimal Fiber100g)
{
    public static FoodProductDto FromModel(FoodProduct input)
    {
        return new FoodProductDto(
            input.Id,
            input.Name,
            Convert.ToDecimal(input.Kcal1000g) / 10,
            Convert.ToDecimal(input.Proteins1000g) / 10,
            Convert.ToDecimal(input.Fats1000g) / 10,
            Convert.ToDecimal(input.Carbs1000g) / 10,
            Convert.ToDecimal(input.Fiber1000g) / 10);
    }
}