using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HealthyMeal.Services
{
  public class DeepSeekAIService : IAIService
  {
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string API_BASE_URL = "https://api.deepseek.com/v1/chat/completions";

    public DeepSeekAIService(HttpClient httpClient)
    {
      _httpClient = httpClient;
      // TODO: Move to secure configuration (User Secrets or encrypted config)
      _apiKey = "sk-359eb4c1e92d4dde937f3c36b3695913"; // Get from https://platform.deepseek.com/

      _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
      _httpClient.Timeout = TimeSpan.FromSeconds(60); // 60s timeout as per PRD
    }

    public async Task<string> GenerateMealPlanAsync(int days, string userPreferences)
    {
      try
      {
        var prompt = BuildMealPlanPrompt(days, userPreferences);
        var response = await CallDeepSeekAPIAsync(prompt);
        return response;
      }
      catch (Exception ex)
      {
        // Log error and return fallback
        return $"{{\"error\": \"Failed to generate meal plan: {ex.Message}\"}}";
      }
    }

    public async Task<string> ModifyMealAsync(string currentMeal, string modificationRequest, string userPreferences)
    {
      try
      {
        var prompt = BuildMealModificationPrompt(currentMeal, modificationRequest, userPreferences);
        var response = await CallDeepSeekAPIAsync(prompt);
        return response;
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
        // Simple health check
        var testPrompt = "Respond with 'OK' if you're working.";
        var response = await CallDeepSeekAPIAsync(testPrompt);
        return response.Contains("OK");
      }
      catch (Exception ex)
      {
        // Log the actual error for debugging
        System.Windows.MessageBox.Show($"AI API Error: {ex.Message}\n\nInner: {ex.InnerException?.Message}",
                                      "Debug - API Error",
                                      System.Windows.MessageBoxButton.OK,
                                      System.Windows.MessageBoxImage.Error);
        return false;
      }
    }

    private async Task<string> CallDeepSeekAPIAsync(string prompt)
    {
      try
      {
        var requestBody = new
        {
          model = "deepseek-chat",
          messages = new[]
            {
                      new { role = "system", content = "You are a professional nutritionist and meal planning assistant. Always respond with valid JSON format." },
                      new { role = "user", content = prompt }
                  },
          max_tokens = 2000,
          temperature = 0.7
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(API_BASE_URL, content);

        // Check if response is successful
        if (!response.IsSuccessStatusCode)
        {
          var errorContent = await response.Content.ReadAsStringAsync();
          throw new Exception($"API Error {response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Extract the actual response from DeepSeek format
        return responseData
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "No response";
      }
      catch (HttpRequestException ex)
      {
        throw new Exception($"Network error: {ex.Message}", ex);
      }
      catch (TaskCanceledException ex)
      {
        throw new Exception($"Request timeout: {ex.Message}", ex);
      }
      catch (JsonException ex)
      {
        throw new Exception($"JSON parsing error: {ex.Message}", ex);
      }
      catch (Exception ex)
      {
        throw new Exception($"Unexpected error: {ex.Message}", ex);
      }
    }

    private string BuildMealPlanPrompt(int days, string userPreferences)
    {
      return $@"
Create a {days}-day meal plan based on these user preferences: {userPreferences}

Requirements:
- 3 meals per day (breakfast, lunch, dinner)
- Include detailed recipes with ingredients and instructions
- Calculate approximate calories, protein, carbs, and fat for each meal
- Exclude any allergens mentioned in preferences
- Stay within the calorie goal if specified

Return the response in this exact JSON format:
{{
  ""mealPlan"": {{
    ""totalDays"": {days},
    ""days"": [
      {{
        ""day"": 1,
        ""date"": ""2024-01-01"",
        ""meals"": [
          {{
            ""type"": ""breakfast"",
            ""name"": ""Scrambled Eggs with Toast"",
            ""ingredients"": [""2 eggs"", ""1 slice bread"", ""butter""],
            ""instructions"": ""Beat eggs, cook in pan with butter..."",
            ""nutrition"": {{
              ""calories"": ""350"",
              ""protein"": ""20g"",
              ""carbs"": ""15g"",
              ""fat"": ""12g""
            }}
          }}
        ]
      }}
    ]
  }}
}}";
    }

    private string BuildMealModificationPrompt(string currentMeal, string modificationRequest, string userPreferences)
    {
      return $@"
Current meal: {currentMeal}
User wants to change it: {modificationRequest}
User preferences: {userPreferences}

Create a new meal that:
- Addresses the user's modification request
- Respects their dietary preferences and allergens
- Has similar nutritional value to the original meal

Return the response in this exact JSON format:
{{
  ""newMeal"": {{
    ""name"": ""New Meal Name"",
    ""ingredients"": [""ingredient1"", ""ingredient2""],
    ""instructions"": ""Step by step cooking instructions"",
    ""nutrition"": {{
      ""calories"": ""350"",
      ""protein"": ""20g"",
      ""carbs"": ""15g"",
      ""fat"": ""12g""
    }}
  }}
}}";
    }
  }
}