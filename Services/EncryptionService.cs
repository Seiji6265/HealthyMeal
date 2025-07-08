using System;
using System.Security.Cryptography;
using System.Text;

namespace HealthyMeal.Services
{
    public interface IEncryptionService
    {
        string EncryptApiKey(string apiKey);
        string DecryptApiKey(string encryptedApiKey);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService()
        {
            // Generujemy stały klucz i IV bazując na identyfikatorze maszyny
            // W produkcji lepiej byłoby użyć Windows DPAPI, ale dla prostoty używamy tej metody
            var machineKey = Environment.MachineName + "HealthyMeal_API_Encryption_2024";
            using (var sha256 = SHA256.Create())
            {
                var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineKey));
                _key = keyBytes;

                // IV tworzymy z pierwszych 16 bajtów klucza
                _iv = new byte[16];
                Array.Copy(keyBytes, _iv, 16);
            }
        }

        public string EncryptApiKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var plainBytes = Encoding.UTF8.GetBytes(apiKey);
                        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                        return Convert.ToBase64String(encryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EncryptionService] Błąd szyfrowania: {ex.Message}");
                throw new InvalidOperationException("Nie udało się zaszyfrować klucza API", ex);
            }
        }

        public string DecryptApiKey(string encryptedApiKey)
        {
            if (string.IsNullOrEmpty(encryptedApiKey))
                return string.Empty;

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = _key;
                    aes.IV = _iv;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        var encryptedBytes = Convert.FromBase64String(encryptedApiKey);
                        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                        return Encoding.UTF8.GetString(decryptedBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EncryptionService] Błąd deszyfrowania: {ex.Message}");
                throw new InvalidOperationException("Nie udało się odszyfrować klucza API", ex);
            }
        }
    }
}