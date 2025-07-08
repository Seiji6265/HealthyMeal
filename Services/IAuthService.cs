using HealthyMeal.DTOs;
using HealthyMeal.Models;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
    /// <summary>
    /// Interface for the main authentication service.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Handles the business logic for user registration.
        /// </summary>
        /// <param name="command">The registration command object.</param>
        /// <returns>The newly created Profile object.</returns>
        Task<Profile> RegisterAsync(RegisterCommand command);

        /// <summary>
        /// Handles the business logic for user login.
        /// </summary>
        /// <param name="email">User's email.</param>
        /// <param name="password">User's password.</param>
        /// <param name="rememberMe">Flag to persist the session.</param>
        /// <returns>A lightweight User object on successful login.</returns>
        Task<User> LoginAsync(string email, string password, bool rememberMe);

        /// <summary>
        /// Attempts to log in a user from a persisted session.
        /// </summary>
        /// <returns>True if auto-login was successful, false otherwise.</returns>
        Task<bool> TryAutoLoginAsync();
    }
}