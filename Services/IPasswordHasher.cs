namespace HealthyMeal.Services
{
    /// <summary>
    /// Defines the contract for a password hashing service.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a plain-text password.
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verifies that a plain-text password matches a stored hash.
        /// </summary>
        bool VerifyPassword(string password, string hash);
    }
}