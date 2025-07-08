using System;
using System.Threading.Tasks;
using Dapper;

namespace HealthyMeal.Services
{
    public interface IApiKeyService
    {
        Task<string> GetGeminiApiKeyAsync();
        Task SetGeminiApiKeyAsync(string apiKey);
        Task<bool> HasValidApiKeyAsync();
    }

    public class ApiKeyService : IApiKeyService
    {
        private readonly DatabaseService _databaseService;
        private readonly IEncryptionService _encryptionService;
        private const string GEMINI_API_KEY_ID = "gemini_api_key";

        public ApiKeyService(DatabaseService databaseService, IEncryptionService encryptionService)
        {
            _databaseService = databaseService;
            _encryptionService = encryptionService;
        }

        public async Task<string> GetGeminiApiKeyAsync()
        {
            try
            {
                using (var connection = _databaseService.GetConnection())
                {
                    var encryptedKey = await connection.QuerySingleOrDefaultAsync<string>(
                        "SELECT encrypted_value FROM api_keys WHERE key_id = @keyId",
                        new { keyId = GEMINI_API_KEY_ID });

                    if (string.IsNullOrEmpty(encryptedKey))
                    {
                        System.Diagnostics.Debug.WriteLine("[ApiKeyService] Brak klucza API w bazie danych");
                        return string.Empty;
                    }

                    var decryptedKey = _encryptionService.DecryptApiKey(encryptedKey);
                    System.Diagnostics.Debug.WriteLine("[ApiKeyService] Klucz API pomyślnie odszyfrowany");
                    return decryptedKey;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiKeyService] Błąd pobierania klucza API: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task SetGeminiApiKeyAsync(string apiKey)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    System.Diagnostics.Debug.WriteLine("[ApiKeyService] Próba zapisania pustego klucza API");
                    return;
                }

                var encryptedKey = _encryptionService.EncryptApiKey(apiKey);

                using (var connection = _databaseService.GetConnection())
                {
                    // Używamy INSERT OR REPLACE żeby zastąpić istniejący klucz
                    await connection.ExecuteAsync(@"
                        INSERT OR REPLACE INTO api_keys (key_id, encrypted_value, created_at, updated_at) 
                        VALUES (@keyId, @encryptedValue, @createdAt, @updatedAt)",
                        new
                        {
                            keyId = GEMINI_API_KEY_ID,
                            encryptedValue = encryptedKey,
                            createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                        });

                    System.Diagnostics.Debug.WriteLine("[ApiKeyService] Klucz API pomyślnie zaszyfrowany i zapisany");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ApiKeyService] Błąd zapisywania klucza API: {ex.Message}");
                throw new InvalidOperationException("Nie udało się zapisać klucza API", ex);
            }
        }

        public async Task<bool> HasValidApiKeyAsync()
        {
            try
            {
                var apiKey = await GetGeminiApiKeyAsync();
                return !string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("AIza");
            }
            catch
            {
                return false;
            }
        }
    }
}