using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.Models;
using HealthyMeal.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HealthyMeal.ViewModels
{
    public partial class RecipeEditorViewModel : ViewModelBase
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly INavigationService _navigationService;
        private readonly IApplicationState _appState;
        private Recipe? _editingRecipe;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveRecipeCommand))]
        private string? _recipeName;

        [ObservableProperty]
        private string? _prepTime;

        [ObservableProperty]
        private string? _ingredients;

        [ObservableProperty]
        private string? _instructions;

        [ObservableProperty]
        private string? _nutrition; // e.g., "calories: 500, protein: 30g"

        [ObservableProperty]
        private string? _errorMessage;

        // Properties for UI state
        public bool IsEditing => _editingRecipe != null;
        public string PageTitle => IsEditing ? "Edit Recipe" : "Add New Recipe";
        public string SaveButtonText => IsEditing ? "Update Recipe" : "Save Recipe";

        public RecipeEditorViewModel(IRecipeRepository recipeRepository, INavigationService navigationService, IApplicationState appState)
        {
            _recipeRepository = recipeRepository;
            _navigationService = navigationService;
            _appState = appState;
        }

        public async Task LoadRecipeAsync(int recipeId)
        {
            try
            {
                _editingRecipe = await _recipeRepository.GetByIdAsync(recipeId);
                if (_editingRecipe != null)
                {
                    RecipeName = _editingRecipe.Name;
                    PrepTime = _editingRecipe.PrepTimeMinutes?.ToString();

                    if (!string.IsNullOrEmpty(_editingRecipe.DataJson))
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        var recipeData = JsonSerializer.Deserialize<RecipeData>(_editingRecipe.DataJson, options);
                        if (recipeData != null)
                        {
                            Ingredients = string.Join(Environment.NewLine, recipeData.Ingredients);
                            Instructions = recipeData.Instructions;
                            // Convert nutrition dictionary to string format
                            if (recipeData.Nutrition?.Any() == true)
                            {
                                Nutrition = string.Join(", ", recipeData.Nutrition.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                            }
                        }
                    }

                    // Notify UI that properties have changed
                    OnPropertyChanged(nameof(IsEditing));
                    OnPropertyChanged(nameof(PageTitle));
                    OnPropertyChanged(nameof(SaveButtonText));
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load recipe: {ex.Message}";
            }
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveRecipe()
        {
            ErrorMessage = string.Empty;

            try
            {
                // Parse nutrition string to dictionary
                var nutritionDict = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(Nutrition))
                {
                    var nutritionPairs = Nutrition.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pair in nutritionPairs)
                    {
                        var keyValue = pair.Split(':', StringSplitOptions.RemoveEmptyEntries);
                        if (keyValue.Length == 2)
                        {
                            nutritionDict[keyValue[0].Trim().ToLower()] = keyValue[1].Trim();
                        }
                    }
                }

                var recipeData = new RecipeData
                {
                    Ingredients = new List<string>(Ingredients?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>()),
                    Instructions = Instructions ?? string.Empty,
                    Nutrition = nutritionDict
                };

                if (IsEditing && _editingRecipe != null)
                {
                    // Update existing recipe
                    _editingRecipe.Name = RecipeName!;
                    _editingRecipe.PrepTimeMinutes = int.TryParse(PrepTime, out var time) ? time : null;
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    _editingRecipe.DataJson = JsonSerializer.Serialize(recipeData, options);

                    bool success = await _recipeRepository.UpdateAsync(_editingRecipe);
                    if (!success)
                    {
                        ErrorMessage = "Failed to update recipe. You can only edit your own custom recipes.";
                        return;
                    }
                }
                else
                {
                    // Create new recipe
                    var newRecipe = new Recipe
                    {
                        OwnerId = _appState.CurrentUser?.Id,
                        IsCustom = true,
                        Name = RecipeName!,
                        PrepTimeMinutes = int.TryParse(PrepTime, out var time) ? time : null,
                        DataJson = JsonSerializer.Serialize(recipeData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        CreatedAt = DateTime.UtcNow
                    };

                    await _recipeRepository.CreateAsync(newRecipe);
                }

                // Navigate back to the recipe list after saving
                _navigationService.NavigateTo("RecipeList");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save recipe: {ex.Message}";
            }
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(RecipeName);
        }

        [RelayCommand]
        private void Cancel()
        {
            _navigationService.NavigateTo("RecipeList");
        }
    }
}