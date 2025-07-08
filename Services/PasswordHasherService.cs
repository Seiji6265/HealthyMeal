using BCrypt.Net;

namespace HealthyMeal.Services
{
    /// <summary>
    /// Implements password hashing and verification using the BCrypt algorithm.
    /// </summary>
    public class PasswordHasherService : IPasswordHasher
    {
        /// <summary>
        /// Hashes a plain-text password.
        /// </summary>
        /// <param name="password">The plain-text password to hash.</param>
        /// <returns>The resulting password hash.</returns>
        public string HashPassword(string password)
        {
            // BCrypt.Net-Next handles salt generation automatically.
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// Verifies that a plain-text password matches a stored hash.
        /// </summary>
        /// <param name="password">The plain-text password to verify.</param>
        /// <param name="hash">The stored hash to verify against.</param>
        /// <returns>True if the password matches the hash, otherwise false.</returns>
        public bool VerifyPassword(string password, string hash)
        {
            // Add an explicit null check to prevent ArgumentNullException from BCrypt library.
            if (string.IsNullOrEmpty(hash))
            {
                return false;
            }
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}