using System;
using System.Threading.Tasks;
using HealthyMeal.Services;
using Dapper;

// TYMCZASOWY PLIK DO DEBUGOWANIA - USUŃ PO ROZWIĄZANIU PROBLEMU

class ApiKeyDebugger
{
    public static async Task DebugApiKey()
    {
        try
        {
            var dbService = new DatabaseService();
            var encryptionService = new EncryptionService();
            var apiKeyService = new ApiKeyService(dbService, encryptionService);

            Console.WriteLine("=== DEBUG KLUCZA API ===");

            // Sprawdź czy klucz istnieje w bazie
            var hasKey = await apiKeyService.HasValidApiKeyAsync();
            Console.WriteLine($"Ma ważny klucz API: {hasKey}");

            // Pobierz klucz
            var apiKey = await apiKeyService.GetGeminiApiKeyAsync();
            Console.WriteLine($"Klucz API: {(string.IsNullOrEmpty(apiKey) ? "BRAK" : $"{apiKey.Substring(0, 6)}...{apiKey.Substring(apiKey.Length - 4)}")}");

            // Sprawdź bezpośrednio w bazie danych
            using (var connection = dbService.GetConnection())
            {
                var count = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM api_keys");
                Console.WriteLine($"Liczba kluczy w bazie: {count}");

                if (count > 0)
                {
                    var keys = connection.Query("SELECT key_id, LENGTH(encrypted_value) as key_length FROM api_keys");
                    foreach (var key in keys)
                    {
                        Console.WriteLine($"Klucz ID: {key.key_id}, Długość zaszyfrowana: {key.key_length}");
                    }
                }
            }

            // Test szyfrowania/odszyfrowywania
            var testKey = "AIzaSyCNqJTriYu2U0KP0rnrhtZgAIDs8i8N3x4";
            var encrypted = encryptionService.EncryptApiKey(testKey);
            var decrypted = encryptionService.DecryptApiKey(encrypted);
            Console.WriteLine($"Test szyfrowania: {testKey == decrypted}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"BŁĄD DEBUGOWANIA: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}