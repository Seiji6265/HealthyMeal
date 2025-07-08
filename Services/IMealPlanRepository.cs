using HealthyMeal.Models;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
    public interface IMealPlanRepository
    {
        /// <summary>
        /// Gets the current active meal plan for a user (not expired).
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Active meal plan or null if none exists</returns>
        Task<MealPlan?> GetCurrentPlanAsync(int userId);

        /// <summary>
        /// Creates a new meal plan for a user. If a plan already exists, it replaces it.
        /// </summary>
        /// <param name="mealPlan">Meal plan to create</param>
        /// <returns>Created meal plan with ID</returns>
        Task<MealPlan> CreateOrReplacePlanAsync(MealPlan mealPlan);

        /// <summary>
        /// Deletes all meal plans for a user.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>Number of deleted plans</returns>
        Task<int> DeleteAllUserPlansAsync(int userId);

        /// <summary>
        /// Checks if user has an active (non-expired) meal plan.
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <returns>True if user has active plan</returns>
        Task<bool> HasActivePlanAsync(int userId);
    }
}