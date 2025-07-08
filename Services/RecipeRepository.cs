using Dapper;
using HealthyMeal.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly DatabaseService _databaseService;

        public RecipeRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<IEnumerable<Recipe>> GetAllAsync(int pageNumber, int pageSize)
        {
            using var connection = _databaseService.GetConnection();
            var offset = (pageNumber - 1) * pageSize;

            var sql = @"
                SELECT 
                    id AS Id,
                    owner_id AS OwnerId,
                    is_custom AS IsCustom,
                    name AS Name,
                    prep_time_minutes AS PrepTimeMinutes,
                    data AS DataJson,
                    created_at AS CreatedAt
                FROM recipes
                ORDER BY name
                LIMIT @PageSize OFFSET @Offset;";

            return await connection.QueryAsync<Recipe>(sql, new { PageSize = pageSize, Offset = offset });
        }

        public async Task<int> GetTotalCountAsync()
        {
            using var connection = _databaseService.GetConnection();
            var sql = "SELECT COUNT(id) FROM recipes;";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<Recipe?> GetByIdAsync(int id)
        {
            using var connection = _databaseService.GetConnection();
            var sql = @"
                SELECT 
                    id AS Id,
                    owner_id AS OwnerId,
                    is_custom AS IsCustom,
                    name AS Name,
                    prep_time_minutes AS PrepTimeMinutes,
                    data AS DataJson,
                    created_at AS CreatedAt
                FROM recipes
                WHERE id = @Id;";

            return await connection.QuerySingleOrDefaultAsync<Recipe>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(Recipe recipe)
        {
            using var connection = _databaseService.GetConnection();
            var sql = @"
                INSERT INTO recipes (owner_id, is_custom, name, prep_time_minutes, data, created_at)
                VALUES (@OwnerId, @IsCustom, @Name, @PrepTimeMinutes, @DataJson, @CreatedAt);
                SELECT last_insert_rowid();";

            return await connection.ExecuteScalarAsync<int>(sql, recipe);
        }

        public async Task<bool> UpdateAsync(Recipe recipe)
        {
            using var connection = _databaseService.GetConnection();
            var sql = @"
                UPDATE recipes 
                SET name = @Name,
                    prep_time_minutes = @PrepTimeMinutes,
                    data = @DataJson
                WHERE id = @Id AND (owner_id = @OwnerId OR is_custom = 1);";

            var rowsAffected = await connection.ExecuteAsync(sql, recipe);
            return rowsAffected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = _databaseService.GetConnection();
            var sql = @"DELETE FROM recipes WHERE id = @Id;";

            var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
            return rowsAffected > 0;
        }

        public async Task<Recipe?> FindByNameAndOwnerAsync(string name, int ownerId)
        {
            using var connection = _databaseService.GetConnection();
            var sql = @"
                SELECT 
                    id AS Id,
                    owner_id AS OwnerId,
                    is_custom AS IsCustom,
                    name AS Name,
                    prep_time_minutes AS PrepTimeMinutes,
                    data AS DataJson,
                    created_at AS CreatedAt
                FROM recipes
                WHERE LOWER(name) = LOWER(@Name) AND owner_id = @OwnerId;";

            return await connection.QuerySingleOrDefaultAsync<Recipe>(sql, new { Name = name, OwnerId = ownerId });
        }
    }
}