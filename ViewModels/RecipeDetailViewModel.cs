using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.Models;
using HealthyMeal.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HealthyMeal.ViewModels
{
    public partial class RecipeDetailViewModel : ViewModelBase
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private Recipe? _recipe;

        [ObservableProperty]
        private RecipeData? _recipeData;

        // Computed properties for binding
        public List<string> Ingredients => RecipeData?.Ingredients ?? new List<string>();
        public string Instructions => RecipeData?.Instructions ?? string.Empty;
        public bool HasNutritionInfo => RecipeData?.Nutrition?.Any() == true;

        public string Calories => GetNutritionValue("calories");
        public string Protein => GetNutritionValue("protein");
        public string Carbs => GetNutritionValue("carbs");
        public string Fat => GetNutritionValue("fat");

        public bool CanEdit => Recipe?.IsCustom == true;
        public bool CanDelete => Recipe != null; // All recipes can be deleted now
        public string RecipeTypeText => Recipe?.IsCustom == true ? "Custom Recipe" : "System Recipe";

        public RecipeDetailViewModel(IRecipeRepository recipeRepository, INavigationService navigationService)
        {
            _recipeRepository = recipeRepository;
            _navigationService = navigationService;
        }

        public async Task LoadRecipeAsync(int recipeId)
        {
            Recipe = await _recipeRepository.GetByIdAsync(recipeId);
            if (Recipe != null && !string.IsNullOrEmpty(Recipe.DataJson))
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                RecipeData = JsonSerializer.Deserialize<RecipeData>(Recipe.DataJson, options);
            }

            // Notify UI that all computed properties may have changed
            OnPropertyChanged(nameof(Ingredients));
            OnPropertyChanged(nameof(Instructions));
            OnPropertyChanged(nameof(HasNutritionInfo));
            OnPropertyChanged(nameof(Calories));
            OnPropertyChanged(nameof(Protein));
            OnPropertyChanged(nameof(Carbs));
            OnPropertyChanged(nameof(Fat));
            OnPropertyChanged(nameof(CanEdit));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(RecipeTypeText));
        }

        private string GetNutritionValue(string key)
        {
            if (RecipeData?.Nutrition != null && RecipeData.Nutrition.TryGetValue(key, out var value))
            {
                return value;
            }
            return "N/A";
        }

        [RelayCommand]
        private void GoBack()
        {
            _navigationService.NavigateTo("RecipeList");
        }

        [RelayCommand]
        private void EditRecipe()
        {
            if (Recipe != null)
            {
                _navigationService.NavigateTo("RecipeEditor", Recipe.Id);
            }
        }

        [RelayCommand]
        private async Task DeleteRecipe()
        {
            if (Recipe == null) return;

            // Simple confirmation - in real app you'd use a proper dialog
            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć przepis '{Recipe.Name}'?\n\nTej operacji nie można cofnąć.",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var success = await _recipeRepository.DeleteAsync(Recipe.Id);

                if (success)
                {
                    System.Windows.MessageBox.Show(
                        "Przepis został pomyślnie usunięty.",
                        "Sukces",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    _navigationService.NavigateTo("RecipeList");
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "Wystąpił błąd podczas usuwania przepisu.",
                        "Błąd",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}