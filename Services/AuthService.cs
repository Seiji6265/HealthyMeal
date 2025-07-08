using HealthyMeal.DTOs;
using HealthyMeal.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HealthyMeal.Services
{
    /// <summary>
    /// Implements the core business logic for user authentication.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly DatabaseService _databaseService;
        private readonly IApplicationState _appState;

        private const string SessionUserIdKey = "session_user_id";
        private const string SessionExpiresAtKey = "session_expires_at";

        public AuthService(IProfileRepository profileRepository, IPasswordHasher passwordHasher, DatabaseService databaseService, IApplicationState appState)
        {
            _profileRepository = profileRepository;
            _passwordHasher = passwordHasher;
            _databaseService = databaseService;
            _appState = appState;
        }

        /// <summary>
        /// Handles the business logic for user registration.
        /// </summary>
        /// <param name="command">The registration command object.</param>
        /// <returns>The newly created Profile object.</returns>
        public async Task<Profile> RegisterAsync(RegisterCommand command)
        {
            // 1. Check if user already exists
            var existingUser = await _profileRepository.FindByEmailAsync(command.Email);
            if (existingUser != null)
            {
                // In a real API, we would throw a specific exception type
                // that the controller would map to a 409 Conflict status code.
                throw new InvalidOperationException("A user with this email already exists.");
            }

            // 2. Hash the password
            var passwordHash = _passwordHasher.HashPassword(command.Password);

            // 3. Serialize preferences to JSON
            var preferencesJson = JsonSerializer.Serialize(command.Preferences);

            // 4. Create the new profile entity
            var newProfile = new Profile
            {
                Email = command.Email,
                PasswordHash = passwordHash,
                PreferencesJson = preferencesJson,
                CreatedAt = DateTime.UtcNow
            };

            // 5. Save the new profile to the database
            var newId = await _profileRepository.CreateAsync(newProfile);
            newProfile.Id = newId;

            return newProfile;
        }

        public async Task<User> LoginAsync(string email, string password, bool rememberMe)
        {
            var profile = await _profileRepository.FindByEmailAsync(email);

            if (profile == null)
            {
                Debug.WriteLine($"[AuthService] Login failed: User '{email}' not found.");
                throw new Exception("Invalid email or password.");
            }

            Debug.WriteLine($"[AuthService] User found. Hash from DB: {profile.PasswordHash}");

            var isPasswordValid = _passwordHasher.VerifyPassword(password, profile.PasswordHash);

            Debug.WriteLine($"[AuthService] Password verification result: {isPasswordValid}");

            if (!isPasswordValid)
            {
                throw new Exception("Invalid email or password.");
            }

            if (rememberMe)
            {
                // Persist session for 3 days
                var expiryDate = DateTime.UtcNow.AddDays(3);
                await _databaseService.SetMetadata(SessionUserIdKey, profile.Id.ToString());
                await _databaseService.SetMetadata(SessionExpiresAtKey, expiryDate.ToString("o")); // ISO 8601 format
            }
            else
            {
                // Clear any existing session
                await _databaseService.SetMetadata(SessionUserIdKey, string.Empty);
                await _databaseService.SetMetadata(SessionExpiresAtKey, string.Empty);
            }

            return new User
            {
                Id = profile.Id,
                Email = profile.Email
            };
        }

        public async Task<bool> TryAutoLoginAsync()
        {
            try
            {
                var userIdStr = await _databaseService.GetMetadata(SessionUserIdKey);
                var expiresAtStr = await _databaseService.GetMetadata(SessionExpiresAtKey);

                if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(expiresAtStr))
                {
                    return false;
                }

                if (!DateTime.TryParse(expiresAtStr, out var expiryDate))
                {
                    return false; // Invalid date format
                }

                if (expiryDate < DateTime.UtcNow)
                {
                    // Session expired
                    await _databaseService.SetMetadata(SessionUserIdKey, string.Empty);
                    await _databaseService.SetMetadata(SessionExpiresAtKey, string.Empty);
                    return false;
                }

                if (!int.TryParse(userIdStr, out var userId))
                {
                    return false; // Invalid user ID format
                }

                var profile = await _profileRepository.GetByIdAsync(userId);
                if (profile == null)
                {
                    return false; // User not found
                }

                _appState.CurrentUser = new User { Id = profile.Id, Email = profile.Email };
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthService] Auto-login failed: {ex.Message}");
                return false;
            }
        }
    }
}