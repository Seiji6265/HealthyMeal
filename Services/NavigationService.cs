using HealthyMeal.ViewModels;
using HealthyMeal.Views.Auth;
using HealthyMeal.Views.Dashboard;
using HealthyMeal.Views.MealPlan;
using HealthyMeal.Views.Profile;
using HealthyMeal.Views.Recipes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace HealthyMeal.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Func<ViewModelBase>> _viewModelFactories;
        private readonly Dictionary<string, Type> _viewTypes;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _viewModelFactories = new Dictionary<string, Func<ViewModelBase>>
            {
                { "Authentication", () => _serviceProvider.GetRequiredService<AuthenticationViewModel>() },
                { "Login", () => _serviceProvider.GetRequiredService<LoginViewModel>() },
                { "Register", () => _serviceProvider.GetRequiredService<RegisterViewModel>() },
                { "Dashboard", () => _serviceProvider.GetRequiredService<DashboardViewModel>() },
                { "RecipeList", () => _serviceProvider.GetRequiredService<RecipeListViewModel>() },
                { "RecipeDetail", () => _serviceProvider.GetRequiredService<RecipeDetailViewModel>() },
                { "RecipeEditor", () => _serviceProvider.GetRequiredService<RecipeEditorViewModel>() },
                { "MealPlanView", () => _serviceProvider.GetRequiredService<MealPlanViewModel>() },
                { "Profile", () => _serviceProvider.GetRequiredService<ProfileViewModel>() }
            };
            _viewTypes = new Dictionary<string, Type>
            {
                { "Authentication", typeof(AuthenticationView) },
                { "Login", typeof(LoginView) },
                { "Register", typeof(RegisterView) },
                { "Dashboard", typeof(DashboardView) },
                { "RecipeList", typeof(RecipeListView) },
                { "RecipeDetail", typeof(RecipeDetailView) },
                { "RecipeEditor", typeof(RecipeEditorView) },
                { "MealPlanView", typeof(MealPlanView) },
                { "Profile", typeof(ProfileView) }
            };
        }

        public void NavigateTo(string viewName, object? parameter = null)
        {
            System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigating to: {viewName}");

            if (!_viewTypes.ContainsKey(viewName) || !_viewModelFactories.ContainsKey(viewName))
            {
                throw new ArgumentException($"View or ViewModel for '{viewName}' not registered.");
            }

            var view = (UserControl)_serviceProvider.GetRequiredService(_viewTypes[viewName]);
            var viewModel = _viewModelFactories[viewName]();
            view.DataContext = viewModel;

            System.Diagnostics.Debug.WriteLine($"[NavigationService] Created view: {view.GetType().Name}, ViewModel: {viewModel.GetType().Name}");

            if (viewModel is RecipeDetailViewModel rdvm && parameter is int recipeId)
            {
                _ = rdvm.LoadRecipeAsync(recipeId);
            }
            else if (viewModel is RecipeEditorViewModel revm && parameter is int editRecipeId)
            {
                _ = revm.LoadRecipeAsync(editRecipeId);
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            (mainWindow.FindName("MainContentControl") as ContentControl)!.Content = view;

            System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigation completed. MainContentControl.Content set to: {view.GetType().Name}");
        }
    }
}