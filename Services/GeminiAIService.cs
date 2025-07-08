using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
  public class GeminiAIService : IAIService
  {
    private readonly HttpClient _httpClient;
    private const string PROXY_BASE_URL = "https://healthymeal-proxy-12345.azurewebsites.net";
    private const string CLIENT_API_KEY = "HM_Beta_2024_SecureKey_XYZ789"; // This will be embedded in the app

    public GeminiAIService(HttpClient httpClient)
    {
      _httpClient = httpClient;
      _httpClient.Timeout = TimeSpan.FromSeconds(60);
      _httpClient.DefaultRequestHeaders.Add("X-API-Key", CLIENT_API_KEY);
    }

    public async Task<string> GenerateMealPlanAsync(int days, string userPreferences)
    {
      try
      {
        System.Diagnostics.Debug.WriteLine($"[GeminiAIService] Generating meal plan for {days} days via proxy");
        System.Diagnostics.Debug.WriteLine($"[GeminiAIService] User preferences: {userPreferences}");

        var requestBody = new
        {
          Days = days,
          UserPreferences = userPreferences
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{PROXY_BASE_URL}/api/generate-plan";
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
          var errorContent = await response.Content.ReadAsStringAsync();
          throw new Exception($"Proxy API Error {response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

        if (responseData.TryGetProperty("response", out var responseText))
        {
          return responseText.GetString() ?? "No response";
        }

        return responseContent; // Fallback to raw response
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"[GeminiAIService] Error generating meal plan: {ex.Message}");
        return $"{{\"error\": \"Failed to generate meal plan: {ex.Message}\"}}";
      }
    }

    public async Task<string> ModifyMealAsync(string currentMeal, string modificationRequest, string userPreferences)
    {
      try
      {
        var requestBody = new
        {
          CurrentMeal = currentMeal,
          ModificationRequest = modificationRequest,
          UserPreferences = userPreferences
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{PROXY_BASE_URL}/api/modify-meal";
        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
          var errorContent = await response.Content.ReadAsStringAsync();
          throw new Exception($"Proxy API Error {response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

        if (responseData.TryGetProperty("response", out var responseText))
        {
          return responseText.GetString() ?? "No response";
        }

        return responseContent;
      }
      catch (Exception ex)
      {
        return $"{{\"error\": \"Failed to modify meal: {ex.Message}\"}}";
      }
    }

    public async Task<bool> IsAvailableAsync()
    {
      try
      {
        var url = $"{PROXY_BASE_URL}/health";
        var response = await _httpClient.GetAsync(url);
        return response.IsSuccessStatusCode;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"[GeminiAIService] Health check failed: {ex.Message}");
        return false;
      }
    }
  }
}