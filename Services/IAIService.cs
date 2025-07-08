using System.Threading.Tasks;

namespace HealthyMeal.Services
{
    public interface IAIService
    {
        /// <summary>
        /// Generates a meal plan for the specified number of days based on user preferences.
        /// </summary>
        /// <param name="days">Number of days to generate the meal plan for</param>
        /// <param name="userPreferences">User dietary preferences and restrictions</param>
        /// <returns>Generated meal plan as JSON string</returns>
        Task<string> GenerateMealPlanAsync(int days, string userPreferences);

        /// <summary>
        /// Modifies a specific meal in a meal plan based on user request.
        /// </summary>
        /// <param name="currentMeal">Current meal to be replaced</param>
        /// <param name="modificationRequest">User's modification request</param>
        /// <param name="userPreferences">User dietary preferences and restrictions</param>
        /// <returns>New meal suggestion as JSON string</returns>
        Task<string> ModifyMealAsync(string currentMeal, string modificationRequest, string userPreferences);

        /// <summary>
        /// Checks if the AI service is available (internet connection, API key, etc.)
        /// </summary>
        /// <returns>True if AI service is available, false otherwise</returns>
        Task<bool> IsAvailableAsync();
    }
}