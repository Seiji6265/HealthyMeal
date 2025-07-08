using System.Collections.Generic;

namespace HealthyMeal.Models
{
    public class RecipeData
    {
        public List<string> Ingredients { get; set; } = new();
        public string Instructions { get; set; } = string.Empty;
        public Dictionary<string, string> Nutrition { get; set; } = new();
    }
}