using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.DTOs;
using HealthyMeal.Models;
using HealthyMeal.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace HealthyMeal.ViewModels
{
  public partial class DashboardViewModel : ViewModelBase
  {
    private readonly INavigationService _navigationService;
    private readonly IAIService _aiService;
    private readonly IMealPlanRepository _mealPlanRepository;
    private readonly IApplicationState _appState;
    private readonly IProfileRepository _profileRepository;

    [ObservableProperty]
    private string _welcomeMessage;

    [ObservableProperty]
    private bool _isGeneratingPlan;

    [ObservableProperty]
    private bool _hasMealPlan;

    [ObservableProperty]
    private int _selectedNumberOfDays = 7; // Domyślnie 7 dni

    public System.Collections.ObjectModel.ObservableCollection<int> DayOptions { get; } = new System.Collections.ObjectModel.ObservableCollection<int>(Enumerable.Range(1, 7));

    public DashboardViewModel(INavigationService navigationService, IApplicationState appState, IAIService aiService, IMealPlanRepository mealPlanRepository, IProfileRepository profileRepository)
    {
      _navigationService = navigationService;
      _aiService = aiService;
      _mealPlanRepository = mealPlanRepository;
      _appState = appState;
      _profileRepository = profileRepository;

      // Personalize the welcome message from the shared state
      var userEmail = appState.CurrentUser?.Email ?? "User";
      WelcomeMessage = $"Welcome, {userEmail}!";

      // Check if user has an active meal plan
      _ = CheckForActiveMealPlan();
    }

    [RelayCommand]
    private async Task NavigateToGeneratePlan()
    {
      await GenerateMealPlan();
    }

    [RelayCommand]
    private void NavigateToMyRecipes()
    {
      _navigationService.NavigateTo("RecipeList");
    }

    [RelayCommand]
    private void NavigateToMyMealPlans()
    {
      System.Diagnostics.Debug.WriteLine("[DashboardViewModel] NavigateToMyMealPlans button clicked!");
      System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] HasMealPlan: {HasMealPlan}");
      _navigationService.NavigateTo("MealPlanView");
    }

    [RelayCommand]
    private void NavigateToProfile()
    {
      _navigationService.NavigateTo("Profile");
    }

    private async Task CheckForActiveMealPlan()
    {
      try
      {
        if (_appState.CurrentUser != null)
        {
          HasMealPlan = await _mealPlanRepository.HasActivePlanAsync(_appState.CurrentUser.Id);
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error checking for active meal plan: {ex.Message}");
        HasMealPlan = false;
      }
    }

    private async Task GenerateMealPlan()
    {
      if (_appState.CurrentUser == null)
      {
        MessageBox.Show("Błąd: Brak zalogowanego użytkownika.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      IsGeneratingPlan = true;

      try
      {
        // Get user preferences from profile
        string userPreferences = await BuildUserPreferencesString();

        // Generate meal plan using AI
        string aiResponse = await _aiService.GenerateMealPlanAsync(SelectedNumberOfDays, userPreferences);

        // Debug: Show raw AI response
        System.Diagnostics.Debug.WriteLine($"Raw AI Response: {aiResponse}");

        // Clean and parse the JSON response
        string cleanedJson = CleanJsonResponse(aiResponse);
        System.Diagnostics.Debug.WriteLine($"Cleaned JSON: {cleanedJson}");

        var mealPlanDto = JsonSerializer.Deserialize<MealPlanDto>(cleanedJson, new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        });

        if (mealPlanDto?.MealPlan == null)
        {
          throw new Exception("Nieprawidłowa odpowiedź z AI - brak danych planu żywieniowego.");
        }

        // Create MealPlan entity
        var mealPlan = new MealPlan
        {
          UserId = _appState.CurrentUser.Id,
          PlanJson = cleanedJson, // Store the cleaned JSON instead of raw AI response
          CreatedAt = DateTime.Now,
          ExpiresAt = DateTime.Now.AddHours(48) // 48h expiry
        };

        // Save to database (replaces existing plan)
        await _mealPlanRepository.CreateOrReplacePlanAsync(mealPlan);

        // Update UI state
        HasMealPlan = true;

        // Show success message
        MessageBox.Show("Twój plan żywieniowy został wygenerowany! Możesz go teraz przeglądać w sekcji 'My Meal Plans'.",
                      "Plan wygenerowany", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      catch (JsonException ex)
      {
        // Try to create a fallback meal plan
        try
        {
          System.Diagnostics.Debug.WriteLine($"JSON parsing failed, creating fallback plan. Error: {ex.Message}");

          var fallbackPlan = CreateFallbackMealPlan();
          var mealPlan = new MealPlan
          {
            UserId = _appState.CurrentUser.Id,
            PlanJson = fallbackPlan,
            CreatedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddHours(48)
          };

          await _mealPlanRepository.CreateOrReplacePlanAsync(mealPlan);
          HasMealPlan = true;

          MessageBox.Show("Wystąpił problem z odpowiedzią AI, ale utworzono przykładowy plan żywieniowy. Możesz go przeglądać w sekcji 'My Meal Plans'.",
                        "Plan utworzony", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch
        {
          // If even fallback fails, show error
          string errorDetails = $"Błąd podczas parsowania odpowiedzi AI: {ex.Message}\n\n";
          errorDetails += "Sprawdź Debug Output w Visual Studio dla pełnej odpowiedzi AI.";

          MessageBox.Show(errorDetails, "Błąd parsowania", MessageBoxButton.OK, MessageBoxImage.Error);
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Błąd podczas generowania planu: {ex.Message}",
                      "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      finally
      {
        IsGeneratingPlan = false;
      }
    }

    private string CleanJsonResponse(string response)
    {
      if (string.IsNullOrWhiteSpace(response))
        return "{}";

      // Remove markdown code blocks if present
      response = response.Trim();
      if (response.StartsWith("```json"))
      {
        response = response.Substring(7); // Remove ```json
      }
      if (response.StartsWith("```"))
      {
        response = response.Substring(3); // Remove ```
      }
      if (response.EndsWith("```"))
      {
        response = response.Substring(0, response.Length - 3); // Remove trailing ```
      }

      // Find the first { and last } to extract JSON
      int firstBrace = response.IndexOf('{');
      int lastBrace = response.LastIndexOf('}');

      if (firstBrace >= 0 && lastBrace > firstBrace)
      {
        response = response.Substring(firstBrace, lastBrace - firstBrace + 1);
      }

      // Enhanced JSON repair for incomplete structures
      response = RepairIncompleteJson(response);

      return response.Trim();
    }

    private string RepairIncompleteJson(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
        return json;

      // Count brackets and braces, respecting string context
      int openBraces = 0, closeBraces = 0;
      int openBrackets = 0, closeBrackets = 0;
      bool inString = false;
      bool escaped = false;

      for (int i = 0; i < json.Length; i++)
      {
        char c = json[i];

        if (escaped)
        {
          escaped = false;
          continue;
        }

        if (c == '\\')
        {
          escaped = true;
          continue;
        }

        if (c == '"' && !escaped)
        {
          inString = !inString;
          continue;
        }

        if (!inString)
        {
          if (c == '{') openBraces++;
          else if (c == '}') closeBraces++;
          else if (c == '[') openBrackets++;
          else if (c == ']') closeBrackets++;
        }
      }

      // Repair the JSON by adding missing closing characters
      string repaired = json;

      // If we're in the middle of a string, close it
      if (inString)
      {
        repaired += "\"";
      }

      // If the JSON ends with a comma, remove it (common in incomplete JSON)
      if (repaired.TrimEnd().EndsWith(","))
      {
        repaired = repaired.TrimEnd().TrimEnd(',');
      }

      // Add missing closing brackets first (arrays)
      while (closeBrackets < openBrackets)
      {
        repaired += "]";
        closeBrackets++;
      }

      // Add missing closing braces (objects)
      while (closeBraces < openBraces)
      {
        repaired += "}";
        closeBraces++;
      }

      return repaired;
    }

    private string CreateFallbackMealPlan()
    {
      return @"{
  ""mealPlan"": {
    ""totalDays"": 2,
    ""days"": [
      {
        ""day"": 1,
        ""date"": ""2024-01-01"",
        ""meals"": [
          {
            ""type"": ""breakfast"",
            ""name"": ""Jajecznica na maśle z tostem"",
            ""ingredients"": [""2 jajka"", ""1 łyżka masła"", ""1 kromka chleba pełnoziarnistego"", ""sól"", ""pieprz""],
            ""instructions"": ""Rozbij jajka do miski, dopraw solą i pieprzem. Rozgrzej masło na patelni i usmaż jajecznicę na średnim ogniu. Podawaj z opiekonym chlebem."",
            ""nutrition"": {
              ""calories"": ""380"",
              ""protein"": ""18g"",
              ""carbs"": ""15g"",
              ""fat"": ""16g""
            }
          },
          {
            ""type"": ""second_breakfast"",
            ""name"": ""Jogurt grecki z miodem"",
            ""ingredients"": [""150g jogurtu greckiego"", ""1 łyżka miodu"", ""garść orzechów włoskich""],
            ""instructions"": ""Wymieszaj jogurt z miodem, posyp orzechami."",
            ""nutrition"": {
              ""calories"": ""180"",
              ""protein"": ""10g"",
              ""carbs"": ""12g"",
              ""fat"": ""8g""
            }
          },
          {
            ""type"": ""lunch"",
            ""name"": ""Kurczak z ryżem i warzywami"",
            ""ingredients"": [""150g piersi kurczaka"", ""100g ryżu brązowego"", ""200g mix warzyw"", ""przyprawy""],
            ""instructions"": ""Ugotuj ryż. Usmaż kurczaka z przyprawami. Dodaj warzywa i duś razem przez 5 minut."",
            ""nutrition"": {
              ""calories"": ""520"",
              ""protein"": ""40g"",
              ""carbs"": ""45g"",
              ""fat"": ""12g""
            }
          },
          {
            ""type"": ""afternoon_snack"",
            ""name"": ""Jabłko z masłem orzechowym"",
            ""ingredients"": [""1 jabłko"", ""1 łyżka masła orzechowego""],
            ""instructions"": ""Pokrój jabłko w plastry, podawaj z masłem orzechowym."",
            ""nutrition"": {
              ""calories"": ""150"",
              ""protein"": ""4g"",
              ""carbs"": ""18g"",
              ""fat"": ""8g""
            }
          },
          {
            ""type"": ""dinner"",
            ""name"": ""Sałatka z łososiem wędzonym"",
            ""ingredients"": [""100g łososia wędzonego"", ""mix sałat"", ""pomidor"", ""ogórek"", ""oliwa z oliwek""],
            ""instructions"": ""Pokrój warzywa, dodaj łososia, polej oliwą i wymieszaj."",
            ""nutrition"": {
              ""calories"": ""320"",
              ""protein"": ""22g"",
              ""carbs"": ""8g"",
              ""fat"": ""18g""
            }
          }
        ]
      },
      {
        ""day"": 2,
        ""date"": ""2024-01-02"",
        ""meals"": [
          {
            ""type"": ""breakfast"",
            ""name"": ""Owsianka z owocami i orzechami"",
            ""ingredients"": [""50g płatków owsianych"", ""200ml mleka migdałowego"", ""1 banan"", ""garść jagód"", ""migdały""],
            ""instructions"": ""Ugotuj płatki owsiane w mleku. Dodaj pokrojonego banana, jagody i migdały."",
            ""nutrition"": {
              ""calories"": ""390"",
              ""protein"": ""12g"",
              ""carbs"": ""55g"",
              ""fat"": ""12g""
            }
          },
          {
            ""type"": ""second_breakfast"",
            ""name"": ""Smoothie proteinowe"",
            ""ingredients"": [""1 banan"", ""200ml mleka"", ""1 łyżka masła orzechowego"", ""szpinak""],
            ""instructions"": ""Zmiksuj wszystkie składniki na gładką masę."",
            ""nutrition"": {
              ""calories"": ""170"",
              ""protein"": ""8g"",
              ""carbs"": ""20g"",
              ""fat"": ""6g""
            }
          },
          {
            ""type"": ""lunch"",
            ""name"": ""Makaron z krewetkami i warzywami"",
            ""ingredients"": [""100g makaronu pełnoziarnistego"", ""150g krewetek"", ""200g cukinii"", ""czosnek"", ""oliwa""],
            ""instructions"": ""Ugotuj makaron. Podsmaż krewetki z czosnkiem, dodaj cukinię. Wymieszaj z makaronem."",
            ""nutrition"": {
              ""calories"": ""540"",
              ""protein"": ""30g"",
              ""carbs"": ""60g"",
              ""fat"": ""16g""
            }
          },
          {
            ""type"": ""afternoon_snack"",
            ""name"": ""Hummus z warzywami"",
            ""ingredients"": [""3 łyżki hummusu"", ""marchewka"", ""papryka"", ""ogórek""],
            ""instructions"": ""Pokrój warzywa w słupki, podawaj z hummussem."",
            ""nutrition"": {
              ""calories"": ""140"",
              ""protein"": ""6g"",
              ""carbs"": ""15g"",
              ""fat"": ""6g""
            }
          },
          {
            ""type"": ""dinner"",
            ""name"": ""Kotlet schabowy z ziemniakami"",
            ""ingredients"": [""120g schabu"", ""200g ziemniaków"", ""surówka z kapusty"", ""olej""],
            ""instructions"": ""Ugotuj ziemniaki. Usmaż schabowego na patelni. Przygotuj surówkę z kapusty."",
            ""nutrition"": {
              ""calories"": ""380"",
              ""protein"": ""28g"",
              ""carbs"": ""35g"",
              ""fat"": ""14g""
            }
          }
        ]
      }
    ]
  }
}";
    }

    private async Task<string> BuildUserPreferencesString()
    {
      try
      {
        if (_appState.CurrentUser == null)
        {
          return "Balanced diet, 2000 calories per day, no specific restrictions";
        }

        var profile = await _profileRepository.GetByIdAsync(_appState.CurrentUser.Id);
        if (profile == null || string.IsNullOrEmpty(profile.PreferencesJson))
        {
          return "Balanced diet, 2000 calories per day, no specific restrictions";
        }

        var preferences = JsonSerializer.Deserialize<UserPreferencesDto>(profile.PreferencesJson, new JsonSerializerOptions
        {
          PropertyNameCaseInsensitive = true
        });

        if (preferences == null)
        {
          return "Balanced diet, 2000 calories per day, no specific restrictions";
        }

        // Build comprehensive preferences string for AI
        var preferencesText = $"Cel kaloryczny: {preferences.CalorieGoal} kcal/dzień";

        if (preferences.Allergens.Any())
        {
          preferencesText += $". ALERGENY (WAŻNE - WYKLUCZYĆ): {string.Join(", ", preferences.Allergens)}";
        }

        if (preferences.HealthConditions.Any())
        {
          preferencesText += $". Choroby/ograniczenia: {string.Join(", ", preferences.HealthConditions)}";
        }

        if (!string.IsNullOrEmpty(preferences.AdditionalPreferences))
        {
          preferencesText += $". Dodatkowe preferencje: {preferences.AdditionalPreferences}";
        }

        return preferencesText;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] Error building preferences string: {ex.Message}");
        return "Balanced diet, 2000 calories per day, no specific restrictions";
      }
    }
  }
}