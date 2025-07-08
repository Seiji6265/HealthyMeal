using HealthyMeal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
    public interface IRecipeRepository
    {
        /// <summary>
        /// Retrieves a paginated list of all recipes.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>A collection of recipes.</returns>
        Task<IEnumerable<Recipe>> GetAllAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Gets the total count of all recipes.
        /// </summary>
        /// <returns>The total number of recipes.</returns>
        Task<int> GetTotalCountAsync();

        /// <summary>
        /// Retrieves a single recipe by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the recipe.</param>
        /// <returns>The recipe with the specified identifier, or null if no recipe exists with that identifier.</returns>
        Task<Recipe?> GetByIdAsync(int id);

        /// <summary>
        /// Creates a new recipe.
        /// </summary>
        /// <param name="recipe">The recipe to create.</param>
        /// <returns>The ID of the newly created recipe.</returns>
        Task<int> CreateAsync(Recipe recipe);

        /// <summary>
        /// Updates an existing recipe.
        /// </summary>
        /// <param name="recipe">The recipe to update.</param>
        /// <returns>True if the update was successful, false otherwise.</returns>
        Task<bool> UpdateAsync(Recipe recipe);

        /// <summary>
        /// Deletes a recipe by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the recipe to delete.</param>
        /// <returns>True if the deletion was successful, false otherwise.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Finds a recipe by name and owner.
        /// </summary>
        /// <param name="name">The name of the recipe.</param>
        /// <param name="ownerId">The ID of the recipe owner.</param>
        /// <returns>The recipe if found, null otherwise.</returns>
        Task<Recipe?> FindByNameAndOwnerAsync(string name, int ownerId);
    }
}