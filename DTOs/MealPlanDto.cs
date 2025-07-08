using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HealthyMeal.DTOs
{
    public class MealPlanDto
    {
        [JsonPropertyName("mealPlan")]
        public MealPlanData MealPlan { get; set; } = new();
    }

    public class MealPlanData
    {
        [JsonPropertyName("totalDays")]
        public int TotalDays { get; set; } = 0;

        [JsonPropertyName("days")]
        public List<DayData> Days { get; set; } = new();
    }

    public class DayData
    {
        [JsonPropertyName("day")]
        public int Day { get; set; } = 0;

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("meals")]
        public List<MealData> Meals { get; set; } = new();

        [JsonPropertyName("instructions")]
        public string Instructions { get; set; } = string.Empty;

        [JsonPropertyName("nutrition")]
        public NutritionData Nutrition { get; set; } = new();
    }

    public class MealData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // breakfast, lunch, dinner

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("ingredients")]
        public List<string> Ingredients { get; set; } = new();

        [JsonPropertyName("instructions")]
        public string Instructions { get; set; } = string.Empty;

        [JsonPropertyName("nutrition")]
        public NutritionData Nutrition { get; set; } = new();
    }

    public class NutritionData
    {
        [JsonPropertyName("calories")]
        public string Calories { get; set; } = string.Empty;

        [JsonPropertyName("protein")]
        public string Protein { get; set; } = string.Empty;

        [JsonPropertyName("carbs")]
        public string Carbs { get; set; } = string.Empty;

        [JsonPropertyName("fat")]
        public string Fat { get; set; } = string.Empty;
    }
}