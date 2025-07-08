using Dapper;
using HealthyMeal.Models;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
    /// <summary>
    /// Manages data access for Profile entities in the database.
    /// </summary>
    public class ProfileRepository : IProfileRepository
    {
        private readonly DatabaseService _databaseService;

        public ProfileRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        /// <summary>
        /// Creates a new user profile in the database.
        /// </summary>
        /// <param name="profile">The profile object to create.</param>
        /// <returns>The ID of the newly created profile.</returns>
        public async Task<int> CreateAsync(Profile profile)
        {
            using var connection = _databaseService.GetConnection();

            var sql = @"
                INSERT INTO profiles (email, password_hash, preferences, created_at)
                VALUES (@Email, @PasswordHash, @PreferencesJson, @CreatedAt)
                RETURNING id;"; // RETURNING id; might not be supported on all SQLite versions, we will use another method if it fails.
                                // For now, let's assume a modern version. A more compatible way is to use last_insert_rowid().

            // Since RETURNING might not work, let's use the standard Dapper way which is more robust
            sql = @"
                INSERT INTO profiles (email, password_hash, preferences, created_at)
                VALUES (@Email, @PasswordHash, @PreferencesJson, @CreatedAt);
                SELECT last_insert_rowid();";

            var newId = await connection.ExecuteScalarAsync<int>(sql, new
            {
                profile.Email,
                profile.PasswordHash,
                profile.PreferencesJson,
                profile.CreatedAt
            });

            return newId;
        }

        /// <summary>
        /// Finds a user profile by their email address.
        /// </summary>
        /// <param name="email">The email address to search for.</param>
        /// <returns>The Profile object if found; otherwise, null.</returns>
        public async Task<Profile?> FindByEmailAsync(string email)
        {
            using var connection = _databaseService.GetConnection();

            var sql = "SELECT id, email, password_hash AS PasswordHash, preferences AS PreferencesJson, created_at AS CreatedAt FROM profiles WHERE LOWER(email) = LOWER(@email)";

            var profile = await connection.QuerySingleOrDefaultAsync<Profile>(sql, new { email });
            return profile;
        }

        /// <summary>
        /// Updates an existing user profile in the database.
        /// </summary>
        /// <param name="profile">The profile object to update.</param>
        public async Task UpdateAsync(Profile profile)
        {
            using var connection = _databaseService.GetConnection();

            var sql = @"
                UPDATE profiles 
                SET preferences = @PreferencesJson
                WHERE id = @Id";

            await connection.ExecuteAsync(sql, new
            {
                profile.Id,
                profile.PreferencesJson
            });
        }

        /// <summary>
        /// Gets a user profile by their ID.
        /// </summary>
        /// <param name="id">The profile ID to search for.</param>
        /// <returns>The Profile object if found; otherwise, null.</returns>
        public async Task<Profile?> GetByIdAsync(int id)
        {
            using var connection = _databaseService.GetConnection();

            var sql = "SELECT id, email, password_hash AS PasswordHash, preferences AS PreferencesJson, created_at AS CreatedAt FROM profiles WHERE id = @id";

            var profile = await connection.QuerySingleOrDefaultAsync<Profile>(sql, new { id });
            return profile;
        }
    }
}