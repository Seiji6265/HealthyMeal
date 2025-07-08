using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthyMeal.DTOs
{
    /// <summary>
    /// Data Transfer Object representing a user's dietary preferences.
    /// Used within the RegisterCommand and for profile updates.
    /// </summary>
    public class UserPreferencesDto
    {
        [Required]
        public List<string> Allergens { get; set; } = new List<string>();

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Calorie goal must be a positive number.")]
        public int CalorieGoal { get; set; } = 0;

        [Required]
        public List<string> HealthConditions { get; set; } = new List<string>();

        /// <summary>
        /// Additional preferences like food dislikes, current fridge contents, etc.
        /// This will be included in AI prompts for better meal plan personalization.
        /// </summary>
        public string AdditionalPreferences { get; set; } = string.Empty;
    }
}