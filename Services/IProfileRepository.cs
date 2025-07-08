using HealthyMeal.Models;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
    /// <summary>
    /// Defines the contract for the profile data repository.
    /// </summary>
    public interface IProfileRepository
    {
        /// <summary>
        /// Finds a user profile by their email address.
        /// </summary>
        Task<Profile?> FindByEmailAsync(string email);

        /// <summary>
        /// Creates a new user profile in the database.
        /// </summary>
        Task<int> CreateAsync(Profile profile);

        /// <summary>
        /// Updates an existing user profile in the database.
        /// </summary>
        Task UpdateAsync(Profile profile);

        /// <summary>
        /// Gets a user profile by their ID.
        /// </summary>
        Task<Profile?> GetByIdAsync(int id);
    }
}