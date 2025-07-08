using Dapper;
using HealthyMeal.Models;
using System;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
    public class MealPlanRepository : IMealPlanRepository
    {
        private readonly DatabaseService _databaseService;

        public MealPlanRepository(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<MealPlan?> GetCurrentPlanAsync(int userId)
        {
            using var connection = _databaseService.GetConnection();

            var sql = @"
                SELECT id AS Id, user_id AS UserId, plan AS PlanJson, 
                       created_at AS CreatedAt, expires_at AS ExpiresAt
                FROM meal_plans 
                WHERE user_id = @userId AND expires_at > @now
                ORDER BY created_at DESC 
                LIMIT 1";

            var result = await connection.QueryFirstOrDefaultAsync<MealPlan>(sql, new
            {
                userId = userId,
                now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
            });

            return result;
        }

        public async Task<MealPlan> CreateOrReplacePlanAsync(MealPlan mealPlan)
        {
            using var connection = _databaseService.GetConnection();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Delete existing plans for this user
                await connection.ExecuteAsync(
                    "DELETE FROM meal_plans WHERE user_id = @userId",
                    new { userId = mealPlan.UserId },
                    transaction);

                // Insert new plan
                var sql = @"
                    INSERT INTO meal_plans (user_id, plan, created_at, expires_at)
                    VALUES (@UserId, @PlanJson, @CreatedAt, @ExpiresAt);
                    SELECT last_insert_rowid();";

                var id = await connection.QuerySingleAsync<int>(sql, new
                {
                    UserId = mealPlan.UserId,
                    PlanJson = mealPlan.PlanJson,
                    CreatedAt = mealPlan.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ExpiresAt = mealPlan.ExpiresAt.ToString("yyyy-MM-dd HH:mm:ss.fff")
                }, transaction);

                transaction.Commit();

                mealPlan.Id = id;
                return mealPlan;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<int> DeleteAllUserPlansAsync(int userId)
        {
            using var connection = _databaseService.GetConnection();

            var deletedCount = await connection.ExecuteAsync(
                "DELETE FROM meal_plans WHERE user_id = @userId",
                new { userId = userId });

            return deletedCount;
        }

        public async Task<bool> HasActivePlanAsync(int userId)
        {
            using var connection = _databaseService.GetConnection();

            var count = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM meal_plans WHERE user_id = @userId AND expires_at > @now",
                new
                {
                    userId = userId,
                    now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                });

            return count > 0;
        }
    }
}