using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.Models;
using HealthyMeal.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace HealthyMeal.ViewModels
{
    public partial class RecipeListViewModel : ViewModelBase
    {
        private readonly IRecipeRepository _recipeRepository;
        private readonly INavigationService _navigationService;
        private const int PageSize = 10;

        [ObservableProperty]
        private ObservableCollection<Recipe> _recipes = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        private int _currentPage = 1;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
        private int _totalPages;

        public RecipeListViewModel(IRecipeRepository recipeRepository, INavigationService navigationService)
        {
            _recipeRepository = recipeRepository;
            _navigationService = navigationService;
            _ = LoadRecipesAsync();
        }

        private async Task LoadRecipesAsync()
        {
            var totalCount = await _recipeRepository.GetTotalCountAsync();
            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            var recipes = await _recipeRepository.GetAllAsync(CurrentPage, PageSize);
            Recipes.Clear();
            foreach (var recipe in recipes)
            {
                Recipes.Add(recipe);
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private async Task PreviousPage()
        {
            CurrentPage--;
            await LoadRecipesAsync();
        }

        private bool CanGoToPreviousPage() => CurrentPage > 1;

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private async Task NextPage()
        {
            CurrentPage++;
            await LoadRecipesAsync();
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;

        [RelayCommand]
        private void ViewRecipeDetails(Recipe recipe)
        {
            if (recipe == null) return;
            _navigationService.NavigateTo("RecipeDetail", recipe.Id);
        }

        [RelayCommand]
        private void GoToDashboard()
        {
            _navigationService.NavigateTo("Dashboard");
        }

        [RelayCommand]
        private void AddRecipe()
        {
            _navigationService.NavigateTo("RecipeEditor");
        }

        [RelayCommand]
        private async Task DeleteRecipe(Recipe recipe)
        {
            if (recipe == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Czy na pewno chcesz usunąć przepis '{recipe.Name}'?\n\nTej operacji nie można cofnąć.",
                "Potwierdzenie usunięcia",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var success = await _recipeRepository.DeleteAsync(recipe.Id);

                if (success)
                {
                    System.Windows.MessageBox.Show(
                        "Przepis został pomyślnie usunięty.",
                        "Sukces",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);

                    // Refresh the list
                    await LoadRecipesAsync();
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