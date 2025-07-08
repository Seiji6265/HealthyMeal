using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.DTOs;
using HealthyMeal.Models;
using HealthyMeal.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace HealthyMeal.ViewModels
{
    public partial class MealPlanViewModel : ViewModelBase
    {
        private readonly IMealPlanRepository _mealPlanRepository;
        private readonly IRecipeRepository _recipeRepository;
        private readonly IApplicationState _appState;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private ObservableCollection<DayPlanViewModel> _days;

        partial void OnDaysChanged(ObservableCollection<DayPlanViewModel> value)
        {
            System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Days collection changed. Count: {value?.Count ?? 0}");
        }

        [ObservableProperty]
        private DayPlanViewModel? _selectedDay;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _hasPlan;

        partial void OnHasPlanChanged(bool value)
        {
            System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] HasPlan changed to: {value}");
        }

        [ObservableProperty]
        private string _errorMessage;

        public MealPlanViewModel(IMealPlanRepository mealPlanRepository, IRecipeRepository recipeRepository,
                                IApplicationState appState, INavigationService navigationService)
        {
            _mealPlanRepository = mealPlanRepository;
            _recipeRepository = recipeRepository;
            _appState = appState;
            _navigationService = navigationService;

            Days = new ObservableCollection<DayPlanViewModel>();
            ErrorMessage = string.Empty;

            System.Diagnostics.Debug.WriteLine("[MealPlanViewModel] Constructor called - ViewModel created");

            // Load meal plan when ViewModel is created
            _ = LoadMealPlan();
        }

        [RelayCommand]
        private void NavigateBack()
        {
            _navigationService.NavigateTo("Dashboard");
        }

        [RelayCommand]
        private async Task AddRecipeToMyRecipes(MealItemViewModel mealItem)
        {
            if (_appState.CurrentUser == null || mealItem == null)
                return;

            try
            {
                // Check if recipe already exists
                var existingRecipe = await _recipeRepository.FindByNameAndOwnerAsync(mealItem.Name, _appState.CurrentUser.Id);

                if (existingRecipe != null)
                {
                    var result = MessageBox.Show(
                        $"Masz już przepis '{mealItem.Name}'. Co chcesz zrobić?",
                        "Przepis już istnieje",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    switch (result)
                    {
                        case MessageBoxResult.Yes: // Replace existing
                            await UpdateExistingRecipe(existingRecipe, mealItem);
                            MessageBox.Show("Przepis został zastąpiony nową wersją!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;
                        case MessageBoxResult.No: // Add as new
                            await CreateNewRecipe(mealItem, GenerateUniqueName(mealItem.Name));
                            MessageBox.Show($"Przepis został dodany jako '{GenerateUniqueName(mealItem.Name)}'!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                            break;
                        default: // Cancel
                            return;
                    }
                }
                else
                {
                    // Recipe doesn't exist, create new
                    await CreateNewRecipe(mealItem, mealItem.Name);
                    MessageBox.Show($"Przepis '{mealItem.Name}' został dodany do Twoich przepisów!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Mark as added to prevent duplicate additions
                mealItem.IsAddedToMyRecipes = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas dodawania przepisu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMealPlan()
        {
            if (_appState.CurrentUser == null)
            {
                ErrorMessage = "Brak zalogowanego użytkownika.";
                System.Diagnostics.Debug.WriteLine("[MealPlanViewModel] Brak zalogowanego użytkownika");
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;
            System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Ładowanie planu dla użytkownika ID: {_appState.CurrentUser.Id}");

            MealPlan? mealPlan = null;
            try
            {
                mealPlan = await _mealPlanRepository.GetCurrentPlanAsync(_appState.CurrentUser.Id);

                if (mealPlan == null)
                {
                    HasPlan = false;
                    ErrorMessage = "Brak aktywnego planu żywieniowego. Wygeneruj nowy plan w Dashboard.";
                    System.Diagnostics.Debug.WriteLine("[MealPlanViewModel] Brak planu w bazie danych");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Znaleziono plan w bazie. ID: {mealPlan.Id}, Expires: {mealPlan.ExpiresAt}");
                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] JSON Length: {mealPlan.PlanJson?.Length ?? 0}");
                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] JSON Preview: {mealPlan.PlanJson?.Substring(0, Math.Min(200, mealPlan.PlanJson?.Length ?? 0))}...");

                if (string.IsNullOrWhiteSpace(mealPlan.PlanJson))
                {
                    ErrorMessage = "Brak danych planu żywieniowego.";
                    System.Diagnostics.Debug.WriteLine("[MealPlanViewModel] PlanJson is null or empty.");
                    HasPlan = false;
                    return;
                }

                // Parse JSON and create day view models
                var mealPlanDto = JsonSerializer.Deserialize<MealPlanDto>(mealPlan.PlanJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (mealPlanDto?.MealPlan?.Days == null)
                {
                    ErrorMessage = "Błąd podczas parsowania planu żywieniowego. Plan może być uszkodzony.";
                    System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Failed to parse meal plan JSON: {mealPlan.PlanJson}");
                    HasPlan = false;
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Pomyślnie sparsowano JSON. Dni: {mealPlanDto.MealPlan.Days.Count}");

                Days.Clear();
                foreach (var day in mealPlanDto.MealPlan.Days.OrderBy(d => d.Day))
                {
                    System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Przetwarzanie dnia {day.Day} z {day.Meals.Count} posiłkami");

                    var dayViewModel = new DayPlanViewModel
                    {
                        DayNumber = day.Day,
                        Date = day.Date,
                        Meals = new ObservableCollection<MealItemViewModel>(
                            day.Meals.Select(m => new MealItemViewModel
                            {
                                Name = m.Name,
                                Type = m.Type,
                                Ingredients = string.Join("\n", m.Ingredients),
                                Instructions = m.Instructions,
                                Calories = m.Nutrition?.Calories ?? "0",
                                Protein = m.Nutrition?.Protein ?? "0g",
                                Carbs = m.Nutrition?.Carbs ?? "0g",
                                Fat = m.Nutrition?.Fat ?? "0g",
                                IsAddedToMyRecipes = false
                            })
                        )
                    };
                    Days.Add(dayViewModel);
                }

                // Select first day by default
                if (Days.Any())
                {
                    SelectedDay = Days.First();
                    System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Wybrano pierwszy dzień: {SelectedDay.DisplayName}");
                }

                HasPlan = true;
                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Plan załadowany pomyślnie. Łącznie dni: {Days.Count}");
            }
            catch (JsonException ex)
            {
                ErrorMessage = "Błąd parsowania planu żywieniowego. Plan może być uszkodzony.";
                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] JSON parsing error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] JSON that failed: {mealPlan?.PlanJson}");
                HasPlan = false;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Błąd podczas ładowania planu: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] General error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] Stack trace: {ex.StackTrace}");
                HasPlan = false;
            }
            finally
            {
                IsLoading = false;
                System.Diagnostics.Debug.WriteLine($"[MealPlanViewModel] LoadMealPlan zakończone. HasPlan: {HasPlan}, Days.Count: {Days.Count}");
            }
        }

        private async Task CreateNewRecipe(MealItemViewModel mealItem, string recipeName)
        {
            var recipeData = new
            {
                ingredients = mealItem.Ingredients.Split('\n', StringSplitOptions.RemoveEmptyEntries),
                instructions = mealItem.Instructions,
                nutrition = new
                {
                    calories = mealItem.Calories,
                    protein = mealItem.Protein,
                    carbs = mealItem.Carbs,
                    fat = mealItem.Fat
                }
            };

            var recipe = new Recipe
            {
                OwnerId = _appState.CurrentUser!.Id,
                IsCustom = true,
                Name = recipeName,
                PrepTimeMinutes = null, // AI recipes don't have prep time
                DataJson = JsonSerializer.Serialize(recipeData),
                CreatedAt = DateTime.Now
            };

            await _recipeRepository.CreateAsync(recipe);
        }

        private async Task UpdateExistingRecipe(Recipe existingRecipe, MealItemViewModel mealItem)
        {
            var recipeData = new
            {
                ingredients = mealItem.Ingredients.Split('\n', StringSplitOptions.RemoveEmptyEntries),
                instructions = mealItem.Instructions,
                nutrition = new
                {
                    calories = mealItem.Calories,
                    protein = mealItem.Protein,
                    carbs = mealItem.Carbs,
                    fat = mealItem.Fat
                }
            };

            existingRecipe.DataJson = JsonSerializer.Serialize(recipeData);
            existingRecipe.PrepTimeMinutes = null; // AI recipes don't have prep time

            await _recipeRepository.UpdateAsync(existingRecipe);
        }

        private string GenerateUniqueName(string baseName)
        {
            return $"{baseName} (AI)";
        }
    }

    // Helper ViewModels for data binding
    public partial class DayPlanViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _dayNumber;

        [ObservableProperty]
        private string _date = string.Empty;

        [ObservableProperty]
        private ObservableCollection<MealItemViewModel> _meals = new();

        public string DisplayName => $"Dzień {DayNumber}";
    }

    public partial class MealItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _type = string.Empty;

        [ObservableProperty]
        private string _ingredients = string.Empty;

        [ObservableProperty]
        private string _instructions = string.Empty;

        [ObservableProperty]
        private string _calories = string.Empty;

        [ObservableProperty]
        private string _protein = string.Empty;

        [ObservableProperty]
        private string _carbs = string.Empty;

        [ObservableProperty]
        private string _fat = string.Empty;

        [ObservableProperty]
        private bool _isAddedToMyRecipes;

        public string MealTypeDisplay => Type switch
        {
            "breakfast" => "Śniadanie",
            "second_breakfast" => "Drugie śniadanie",
            "lunch" => "Obiad",
            "afternoon_snack" => "Podwieczorek",
            "dinner" => "Kolacja",
            _ => Type
        };

        public string NutritionSummary => $"{Calories} kcal | {Protein} białka | {Carbs} węgl. | {Fat} tł.";
    }
}