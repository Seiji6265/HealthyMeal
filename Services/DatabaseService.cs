using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Windows;
using Dapper;
using System.Threading.Tasks;

// Zakładając, że główna przestrzeń nazw Twojego projektu to HealthyMeal
namespace HealthyMeal.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public DatabaseService()
        {
            // Przechowujemy bazę danych w folderze danych aplikacji, co jest najlepszą praktyką.
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "HealthyMeal");
            Directory.CreateDirectory(appFolder); // Tworzymy folder, jeśli nie istnieje

            _databasePath = Path.Combine(appFolder, "HealthyMeal.db");

            // TODO: W wersji produkcyjnej hasło powinno być zarządzane bezpieczniej,
            // np. przy użyciu Windows DPAPI, a nie zapisane w kodzie.
            // Na potrzeby developmentu jest to wystarczające.
            var password = "TwojeSuperTajneHaslo123!";
            _connectionString = $"Data Source={_databasePath};Password={password};";

            InitializeDatabase();
        }

        // Metoda do pobierania nowego, otwartego połączenia z bazą danych
        public SqliteConnection GetConnection()
        {
            var connection = new SqliteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public async Task SetMetadata(string key, string value)
        {
            using (var connection = GetConnection())
            {
                await connection.ExecuteAsync(
                    "INSERT OR REPLACE INTO app_metadata (key, value) VALUES (@key, @value)",
                    new { key, value });
            }
        }

        public async Task<string?> GetMetadata(string key)
        {
            using (var connection = GetConnection())
            {
                return await connection.ExecuteScalarAsync<string?>(
                    "SELECT value FROM app_metadata WHERE key = @key",
                    new { key });
            }
        }

        private void InitializeDatabase()
        {
            // Tworzymy bazę i jej schemat tylko wtedy, gdy plik bazy danych nie istnieje.
            if (File.Exists(_databasePath))
            {
                // Database exists, check if we need to migrate
                MigrateDatabaseIfNeeded();
                // Check if we need to update system recipes
                UpdateSystemRecipesIfNeeded();
                // Clean up expired meal plans
                CleanupExpiredMealPlans();
                return;
            }

            try
            {
                using (var connection = GetConnection())
                {
                    var command = connection.CreateCommand();

                    // Łączymy wszystkie polecenia CREATE TABLE i CREATE INDEX w jeden duży skrypt.
                    // Użycie transakcji sprawia, że albo wszystko się powiedzie, albo nic.
                    command.CommandText = @"
                        BEGIN TRANSACTION;

                        -- Tabela metadanych
                        CREATE TABLE IF NOT EXISTS app_metadata (
                            key   TEXT PRIMARY KEY,
                            value TEXT NOT NULL
                        );
                        INSERT INTO app_metadata (key, value) VALUES ('db_version', '1');

                        -- Tabela profili użytkowników
                        CREATE TABLE IF NOT EXISTS profiles (
                            id             INTEGER PRIMARY KEY AUTOINCREMENT,
                            email          TEXT    NOT NULL UNIQUE,
                            password_hash  TEXT    NOT NULL,
                            preferences    TEXT    NOT NULL,
                            created_at     TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f', 'now'))
                        );

                        -- Tabela przepisów
                        CREATE TABLE IF NOT EXISTS recipes (
                            id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                            owner_id           INTEGER NULL,
                            is_custom          INTEGER NOT NULL DEFAULT 0,
                            name               TEXT    NOT NULL,
                            prep_time_minutes  INTEGER NULL,
                            data               TEXT    NOT NULL,
                            created_at         TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f', 'now')),
                            FOREIGN KEY (owner_id) REFERENCES profiles(id) ON DELETE CASCADE
                        );

                        -- Tabela planów żywieniowych
                        CREATE TABLE IF NOT EXISTS meal_plans (
                            id        INTEGER PRIMARY KEY AUTOINCREMENT,
                            user_id   INTEGER NOT NULL,
                            plan      TEXT    NOT NULL,
                            created_at TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f', 'now')),
                            expires_at TEXT    NOT NULL,
                            FOREIGN KEY (user_id) REFERENCES profiles(id) ON DELETE CASCADE
                        );

                        -- Tabela zaszyfrowanych kluczy API
                        CREATE TABLE IF NOT EXISTS api_keys (
                            key_id         TEXT PRIMARY KEY,
                            encrypted_value TEXT NOT NULL,
                            created_at     TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f', 'now')),
                            updated_at     TEXT NOT NULL DEFAULT (strftime('%Y-%m-%d %H:%M:%f', 'now'))
                        );

                        -- Indeksy dla wydajności
                        CREATE UNIQUE INDEX IF NOT EXISTS ux_profiles_email ON profiles(LOWER(email));
                        CREATE UNIQUE INDEX IF NOT EXISTS ux_recipes_name_owner ON recipes(LOWER(name), owner_id);
                        CREATE INDEX IF NOT EXISTS ix_recipes_owner_id ON recipes(owner_id);
                        CREATE INDEX IF NOT EXISTS ix_recipes_prep_time ON recipes(prep_time_minutes);
                        CREATE INDEX IF NOT EXISTS ix_meal_plans_user_id ON meal_plans(user_id);

                        COMMIT;
                    ";

                    command.ExecuteNonQuery();
                    SeedData(connection);
                }
                // Temporary success message for diagnostics
                MessageBox.Show("Database initialized successfully!", "Diagnostics", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // If anything goes wrong, show a detailed error message.
                MessageBox.Show($"Failed to initialize the database: {ex.ToString()}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SeedData(SqliteConnection connection)
        {
            // Check if recipes already exist
            var recipeCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM recipes");
            if (recipeCount > 0)
            {
                return; // Data already seeded
            }

            var sampleRecipes = new[]
            {
                new { Name = "Scrambled Eggs", PrepTime = 10, Data = "{ \"ingredients\": [\"2 large eggs\", \"1 tbsp butter\", \"salt to taste\", \"pepper to taste\"], \"instructions\": \"Beat eggs in a bowl. Heat butter in a non-stick pan over medium-low heat. Add eggs and gently stir continuously until creamy and set. Season with salt and pepper.\", \"nutrition\": { \"calories\": \"150\", \"protein\": \"12g\", \"carbs\": \"1g\", \"fat\": \"11g\" } }" },
                new { Name = "Grilled Chicken Salad", PrepTime = 20, Data = "{ \"ingredients\": [\"100g chicken breast\", \"2 cups mixed lettuce\", \"1 medium tomato\", \"1 tbsp olive oil\", \"1 tsp lemon juice\"], \"instructions\": \"Season chicken with salt and pepper. Grill for 6-7 minutes each side. Let rest, then slice. Mix lettuce and diced tomato in bowl. Top with sliced chicken and drizzle with olive oil and lemon juice.\", \"nutrition\": { \"calories\": \"350\", \"protein\": \"25g\", \"carbs\": \"8g\", \"fat\": \"15g\" } }" },
                new { Name = "Oatmeal with Berries", PrepTime = 5, Data = "{ \"ingredients\": [\"50g rolled oats\", \"100ml milk\", \"1/2 cup mixed berries\", \"1 tsp honey\"], \"instructions\": \"Cook oats with milk in microwave for 2-3 minutes or until thick. Stir in honey. Top with fresh berries.\", \"nutrition\": { \"calories\": \"250\", \"protein\": \"8g\", \"carbs\": \"45g\", \"fat\": \"4g\" } }" },
                new { Name = "Pasta with Tomato Sauce", PrepTime = 25, Data = "{ \"ingredients\": [\"100g whole wheat pasta\", \"200g canned tomatoes\", \"2 cloves garlic\", \"1 tbsp olive oil\", \"fresh basil\"], \"instructions\": \"Cook pasta according to package instructions. Heat oil in pan, add minced garlic. Add tomatoes and simmer 10 minutes. Toss with pasta and fresh basil.\", \"nutrition\": { \"calories\": \"400\", \"protein\": \"14g\", \"carbs\": \"75g\", \"fat\": \"8g\" } }" },
                new { Name = "Tuna Sandwich", PrepTime = 7, Data = "{ \"ingredients\": [\"1 can tuna in water\", \"2 slices whole grain bread\", \"1 tbsp mayonnaise\", \"lettuce leaves\", \"1 tomato slice\"], \"instructions\": \"Drain tuna and mix with mayonnaise. Toast bread if desired. Layer tuna mixture, lettuce, and tomato between bread slices.\", \"nutrition\": { \"calories\": \"300\", \"protein\": \"25g\", \"carbs\": \"25g\", \"fat\": \"12g\" } }" },
                new { Name = "Protein Shake", PrepTime = 3, Data = "{ \"ingredients\": [\"1 scoop protein powder\", \"250ml water\", \"1/2 banana\", \"ice cubes\"], \"instructions\": \"Add all ingredients to blender. Blend until smooth, about 30 seconds. Serve immediately.\", \"nutrition\": { \"calories\": \"120\", \"protein\": \"20g\", \"carbs\": \"8g\", \"fat\": \"1g\" } }" },
                new { Name = "Greek Yogurt with Honey", PrepTime = 2, Data = "{ \"ingredients\": [\"150g Greek yogurt\", \"1 tbsp honey\", \"chopped nuts (optional)\"], \"instructions\": \"Place yogurt in bowl. Drizzle with honey. Top with nuts if desired.\", \"nutrition\": { \"calories\": \"180\", \"protein\": \"15g\", \"carbs\": \"20g\", \"fat\": \"3g\" } }" },
                new { Name = "Steak with Asparagus", PrepTime = 30, Data = "{ \"ingredients\": [\"150g beef steak\", \"200g asparagus\", \"1 tbsp olive oil\", \"salt\", \"pepper\"], \"instructions\": \"Season steak with salt and pepper. Heat oil in pan over high heat. Cook steak 3-4 minutes each side for medium-rare. Rest 5 minutes. Steam asparagus 3-4 minutes until tender.\", \"nutrition\": { \"calories\": \"500\", \"protein\": \"35g\", \"carbs\": \"6g\", \"fat\": \"25g\" } }" },
                new { Name = "Salmon with Quinoa", PrepTime = 25, Data = "{ \"ingredients\": [\"120g salmon fillet\", \"50g quinoa\", \"100ml vegetable broth\", \"lemon wedge\", \"dill\"], \"instructions\": \"Rinse quinoa. Cook in broth for 15 minutes until fluffy. Season salmon with salt, pepper, and dill. Pan-fry 4 minutes each side. Serve with lemon.\", \"nutrition\": { \"calories\": \"450\", \"protein\": \"30g\", \"carbs\": \"35g\", \"fat\": \"18g\" } }" },
                new { Name = "Cottage Cheese with Veggies", PrepTime = 5, Data = "{ \"ingredients\": [\"150g cottage cheese\", \"1/2 cucumber\", \"3 radishes\", \"chives\", \"pepper\"], \"instructions\": \"Dice cucumber and radishes. Mix with cottage cheese. Season with pepper and chopped chives.\", \"nutrition\": { \"calories\": \"130\", \"protein\": \"18g\", \"carbs\": \"6g\", \"fat\": \"3g\" } }" },
                new { Name = "Apple Slices with Peanut Butter", PrepTime = 4, Data = "{ \"ingredients\": [\"1 medium apple\", \"2 tbsp natural peanut butter\"], \"instructions\": \"Wash and core apple. Cut into wedges. Serve with peanut butter for dipping.\", \"nutrition\": { \"calories\": \"220\", \"protein\": \"8g\", \"carbs\": \"25g\", \"fat\": \"12g\" } }" },
                new { Name = "Rice with Chicken and Broccoli", PrepTime = 30, Data = "{ \"ingredients\": [\"100g chicken breast\", \"50g brown rice\", \"150g broccoli\", \"1 tbsp soy sauce\", \"1 tsp sesame oil\"], \"instructions\": \"Cook rice according to package instructions. Steam broccoli 5 minutes. Cut chicken into strips and stir-fry 5-6 minutes. Combine all with soy sauce and sesame oil.\", \"nutrition\": { \"calories\": \"420\", \"protein\": \"28g\", \"carbs\": \"45g\", \"fat\": \"12g\" } }" },
                new { Name = "Simple Green Smoothie", PrepTime = 6, Data = "{ \"ingredients\": [\"1 cup spinach\", \"1 banana\", \"200ml almond milk\", \"1 tbsp chia seeds\"], \"instructions\": \"Add all ingredients to blender. Blend until smooth, about 1 minute. Let sit 2 minutes for chia seeds to expand.\", \"nutrition\": { \"calories\": \"190\", \"protein\": \"6g\", \"carbs\": \"30g\", \"fat\": \"6g\" } }" },
                new { Name = "Lentil Soup", PrepTime = 40, Data = "{ \"ingredients\": [\"100g red lentils\", \"400ml vegetable broth\", \"1 onion\", \"2 carrots\", \"1 tsp cumin\"], \"instructions\": \"Dice onion and carrots. Sauté in pot 5 minutes. Add lentils, broth, and cumin. Simmer 25 minutes until lentils are soft. Season to taste.\", \"nutrition\": { \"calories\": \"280\", \"protein\": \"18g\", \"carbs\": \"45g\", \"fat\": \"2g\" } }" },
                new { Name = "Turkey Wrap", PrepTime = 8, Data = "{ \"ingredients\": [\"80g sliced turkey breast\", \"1 large tortilla\", \"2 lettuce leaves\", \"1 tomato\", \"1 tbsp hummus\"], \"instructions\": \"Spread hummus on tortilla. Layer with lettuce, sliced tomato, and turkey. Roll tightly and cut in half.\", \"nutrition\": { \"calories\": \"320\", \"protein\": \"22g\", \"carbs\": \"35g\", \"fat\": \"8g\" } }" }
            };

            var sql = @"INSERT INTO recipes (is_custom, name, prep_time_minutes, data, created_at) 
                        VALUES (0, @Name, @PrepTime, @Data, strftime('%Y-%m-%d %H:%M:%f', 'now'));";

            connection.Execute(sql, sampleRecipes);
        }

        private void UpdateSystemRecipesIfNeeded()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    // Check if system recipes have proper data by looking for recipes without instructions
                    var recipesNeedingUpdate = connection.ExecuteScalar<int>(
                        @"SELECT COUNT(*) FROM recipes 
                          WHERE is_custom = 0 
                          AND (data NOT LIKE '%instructions%' OR data LIKE '%""ingredients"": [""2 eggs%')");

                    if (recipesNeedingUpdate > 0)
                    {
                        // System recipes need updating
                        UpdateSystemRecipes(connection);
                        MessageBox.Show($"Zaktualizowano {recipesNeedingUpdate} przepisów systemowych o pełne dane!", "Aktualizacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas aktualizacji przepisów systemowych: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSystemRecipes(SqliteConnection connection)
        {
            var updatedRecipes = new[]
            {
                new { Name = "Scrambled Eggs", PrepTime = 10, Data = "{ \"ingredients\": [\"2 large eggs\", \"1 tbsp butter\", \"salt to taste\", \"pepper to taste\"], \"instructions\": \"Beat eggs in a bowl. Heat butter in a non-stick pan over medium-low heat. Add eggs and gently stir continuously until creamy and set. Season with salt and pepper.\", \"nutrition\": { \"calories\": \"150\", \"protein\": \"12g\", \"carbs\": \"1g\", \"fat\": \"11g\" } }" },
                new { Name = "Grilled Chicken Salad", PrepTime = 20, Data = "{ \"ingredients\": [\"100g chicken breast\", \"2 cups mixed lettuce\", \"1 medium tomato\", \"1 tbsp olive oil\", \"1 tsp lemon juice\"], \"instructions\": \"Season chicken with salt and pepper. Grill for 6-7 minutes each side. Let rest, then slice. Mix lettuce and diced tomato in bowl. Top with sliced chicken and drizzle with olive oil and lemon juice.\", \"nutrition\": { \"calories\": \"350\", \"protein\": \"25g\", \"carbs\": \"8g\", \"fat\": \"15g\" } }" },
                new { Name = "Oatmeal with Berries", PrepTime = 5, Data = "{ \"ingredients\": [\"50g rolled oats\", \"100ml milk\", \"1/2 cup mixed berries\", \"1 tsp honey\"], \"instructions\": \"Cook oats with milk in microwave for 2-3 minutes or until thick. Stir in honey. Top with fresh berries.\", \"nutrition\": { \"calories\": \"250\", \"protein\": \"8g\", \"carbs\": \"45g\", \"fat\": \"4g\" } }" },
                new { Name = "Pasta with Tomato Sauce", PrepTime = 25, Data = "{ \"ingredients\": [\"100g whole wheat pasta\", \"200g canned tomatoes\", \"2 cloves garlic\", \"1 tbsp olive oil\", \"fresh basil\"], \"instructions\": \"Cook pasta according to package instructions. Heat oil in pan, add minced garlic. Add tomatoes and simmer 10 minutes. Toss with pasta and fresh basil.\", \"nutrition\": { \"calories\": \"400\", \"protein\": \"14g\", \"carbs\": \"75g\", \"fat\": \"8g\" } }" },
                new { Name = "Tuna Sandwich", PrepTime = 7, Data = "{ \"ingredients\": [\"1 can tuna in water\", \"2 slices whole grain bread\", \"1 tbsp mayonnaise\", \"lettuce leaves\", \"1 tomato slice\"], \"instructions\": \"Drain tuna and mix with mayonnaise. Toast bread if desired. Layer tuna mixture, lettuce, and tomato between bread slices.\", \"nutrition\": { \"calories\": \"300\", \"protein\": \"25g\", \"carbs\": \"25g\", \"fat\": \"12g\" } }" },
                new { Name = "Protein Shake", PrepTime = 3, Data = "{ \"ingredients\": [\"1 scoop protein powder\", \"250ml water\", \"1/2 banana\", \"ice cubes\"], \"instructions\": \"Add all ingredients to blender. Blend until smooth, about 30 seconds. Serve immediately.\", \"nutrition\": { \"calories\": \"120\", \"protein\": \"20g\", \"carbs\": \"8g\", \"fat\": \"1g\" } }" },
                new { Name = "Greek Yogurt with Honey", PrepTime = 2, Data = "{ \"ingredients\": [\"150g Greek yogurt\", \"1 tbsp honey\", \"chopped nuts (optional)\"], \"instructions\": \"Place yogurt in bowl. Drizzle with honey. Top with nuts if desired.\", \"nutrition\": { \"calories\": \"180\", \"protein\": \"15g\", \"carbs\": \"20g\", \"fat\": \"3g\" } }" },
                new { Name = "Steak with Asparagus", PrepTime = 30, Data = "{ \"ingredients\": [\"150g beef steak\", \"200g asparagus\", \"1 tbsp olive oil\", \"salt\", \"pepper\"], \"instructions\": \"Season steak with salt and pepper. Heat oil in pan over high heat. Cook steak 3-4 minutes each side for medium-rare. Rest 5 minutes. Steam asparagus 3-4 minutes until tender.\", \"nutrition\": { \"calories\": \"500\", \"protein\": \"35g\", \"carbs\": \"6g\", \"fat\": \"25g\" } }" },
                new { Name = "Salmon with Quinoa", PrepTime = 25, Data = "{ \"ingredients\": [\"120g salmon fillet\", \"50g quinoa\", \"100ml vegetable broth\", \"lemon wedge\", \"dill\"], \"instructions\": \"Rinse quinoa. Cook in broth for 15 minutes until fluffy. Season salmon with salt, pepper, and dill. Pan-fry 4 minutes each side. Serve with lemon.\", \"nutrition\": { \"calories\": \"450\", \"protein\": \"30g\", \"carbs\": \"35g\", \"fat\": \"18g\" } }" },
                new { Name = "Cottage Cheese with Veggies", PrepTime = 5, Data = "{ \"ingredients\": [\"150g cottage cheese\", \"1/2 cucumber\", \"3 radishes\", \"chives\", \"pepper\"], \"instructions\": \"Dice cucumber and radishes. Mix with cottage cheese. Season with pepper and chopped chives.\", \"nutrition\": { \"calories\": \"130\", \"protein\": \"18g\", \"carbs\": \"6g\", \"fat\": \"3g\" } }" },
                new { Name = "Apple Slices with Peanut Butter", PrepTime = 4, Data = "{ \"ingredients\": [\"1 medium apple\", \"2 tbsp natural peanut butter\"], \"instructions\": \"Wash and core apple. Cut into wedges. Serve with peanut butter for dipping.\", \"nutrition\": { \"calories\": \"220\", \"protein\": \"8g\", \"carbs\": \"25g\", \"fat\": \"12g\" } }" },
                new { Name = "Rice with Chicken and Broccoli", PrepTime = 30, Data = "{ \"ingredients\": [\"100g chicken breast\", \"50g brown rice\", \"150g broccoli\", \"1 tbsp soy sauce\", \"1 tsp sesame oil\"], \"instructions\": \"Cook rice according to package instructions. Steam broccoli 5 minutes. Cut chicken into strips and stir-fry 5-6 minutes. Combine all with soy sauce and sesame oil.\", \"nutrition\": { \"calories\": \"420\", \"protein\": \"28g\", \"carbs\": \"45g\", \"fat\": \"12g\" } }" },
                new { Name = "Simple Green Smoothie", PrepTime = 6, Data = "{ \"ingredients\": [\"1 cup spinach\", \"1 banana\", \"200ml almond milk\", \"1 tbsp chia seeds\"], \"instructions\": \"Add all ingredients to blender. Blend until smooth, about 1 minute. Let sit 2 minutes for chia seeds to expand.\", \"nutrition\": { \"calories\": \"190\", \"protein\": \"6g\", \"carbs\": \"30g\", \"fat\": \"6g\" } }" },
                new { Name = "Lentil Soup", PrepTime = 40, Data = "{ \"ingredients\": [\"100g red lentils\", \"400ml vegetable broth\", \"1 onion\", \"2 carrots\", \"1 tsp cumin\"], \"instructions\": \"Dice onion and carrots. Sauté in pot 5 minutes. Add lentils, broth, and cumin. Simmer 25 minutes until lentils are soft. Season to taste.\", \"nutrition\": { \"calories\": \"280\", \"protein\": \"18g\", \"carbs\": \"45g\", \"fat\": \"2g\" } }" },
                new { Name = "Turkey Wrap", PrepTime = 8, Data = "{ \"ingredients\": [\"80g sliced turkey breast\", \"1 large tortilla\", \"2 lettuce leaves\", \"1 tomato\", \"1 tbsp hummus\"], \"instructions\": \"Spread hummus on tortilla. Layer with lettuce, sliced tomato, and turkey. Roll tightly and cut in half.\", \"nutrition\": { \"calories\": \"320\", \"protein\": \"22g\", \"carbs\": \"35g\", \"fat\": \"8g\" } }" }
            };

            var sql = @"UPDATE recipes 
                        SET data = @Data, prep_time_minutes = @PrepTime 
                        WHERE is_custom = 0 AND name = @Name;";

            connection.Execute(sql, updatedRecipes);
        }

        private void MigrateDatabaseIfNeeded()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    // Check if expires_at column exists
                    var columnExists = connection.ExecuteScalar<int>(
                        @"SELECT COUNT(*) FROM pragma_table_info('meal_plans') 
                          WHERE name = 'expires_at'") > 0;

                    if (!columnExists)
                    {
                        // Add expires_at column
                        connection.Execute("ALTER TABLE meal_plans ADD COLUMN expires_at TEXT NOT NULL DEFAULT '2024-01-01 00:00:00.000'");

                        // Update existing meal plans to expire in 48h from now
                        var futureExpiry = DateTime.Now.AddHours(48).ToString("yyyy-MM-dd HH:mm:ss.fff");
                        connection.Execute("UPDATE meal_plans SET expires_at = @expiry", new { expiry = futureExpiry });

                        System.Diagnostics.Debug.WriteLine("Dodano kolumnę expires_at do tabeli meal_plans.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas migracji bazy danych: {ex.Message}");
                MessageBox.Show($"Błąd podczas migracji bazy danych: {ex.Message}", "Błąd migracji", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CleanupExpiredMealPlans()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    var deletedCount = connection.Execute(
                        "DELETE FROM meal_plans WHERE expires_at < @now",
                        new { now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") });

                    if (deletedCount > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Usunięto {deletedCount} wygasłych planów żywieniowych.");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas czyszczenia wygasłych planów: {ex.Message}");
            }
        }

        private void CleanupCorruptedMealPlans()
        {
            try
            {
                using (var connection = GetConnection())
                {
                    // Find meal plans with markdown formatting or invalid JSON
                    var corruptedPlans = connection.Query<(int id, string plan)>(
                        "SELECT id, plan FROM meal_plans WHERE plan LIKE '```%' OR plan LIKE '%```%'");

                    foreach (var (id, plan) in corruptedPlans)
                    {
                        // Clean the JSON
                        string cleanedJson = CleanJsonFromMarkdown(plan);

                        // Try to validate it's proper JSON
                        try
                        {
                            System.Text.Json.JsonSerializer.Deserialize<object>(cleanedJson);

                            // If valid, update the record
                            connection.Execute("UPDATE meal_plans SET plan = @cleanedJson WHERE id = @id",
                                             new { cleanedJson, id });

                            System.Diagnostics.Debug.WriteLine($"Naprawiono plan żywieniowy ID: {id}");
                        }
                        catch
                        {
                            // If still invalid, delete the corrupted plan
                            connection.Execute("DELETE FROM meal_plans WHERE id = @id", new { id });
                            System.Diagnostics.Debug.WriteLine($"Usunięto zepsuty plan żywieniowy ID: {id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Błąd podczas czyszczenia zepsutych planów: {ex.Message}");
            }
        }

        private string CleanJsonFromMarkdown(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "{}";

            // Remove markdown code blocks
            json = json.Trim();
            if (json.StartsWith("```json"))
                json = json.Substring(7);
            if (json.StartsWith("```"))
                json = json.Substring(3);
            if (json.EndsWith("```"))
                json = json.Substring(0, json.Length - 3);

            // Find first { and last }
            int firstBrace = json.IndexOf('{');
            int lastBrace = json.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
                json = json.Substring(firstBrace, lastBrace - firstBrace + 1);

            return json.Trim();
        }
    }
}