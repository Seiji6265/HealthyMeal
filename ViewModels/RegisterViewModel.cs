using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.DTOs;
using HealthyMeal.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace HealthyMeal.ViewModels
{
    public partial class RegisterViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _email;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _password;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _allergens;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string? _calorieGoal;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string _healthConditions;

        [ObservableProperty]
        private string _errorMessage;

        public RegisterViewModel(IAuthService authService, INavigationService navigationService)
        {
            _authService = authService;
            _navigationService = navigationService;
            _email = string.Empty;
            _password = string.Empty;
            _allergens = string.Empty;
            _healthConditions = string.Empty;
            _errorMessage = string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task Register()
        {
            ErrorMessage = string.Empty;

            if (!int.TryParse(CalorieGoal, out int calorieGoalValue))
            {
                ErrorMessage = "Please enter a valid number for calorie goal.";
                return;
            }

            try
            {
                var command = new RegisterCommand
                {
                    Email = this.Email,
                    Password = this.Password,
                    Preferences = new UserPreferencesDto
                    {
                        Allergens = new System.Collections.Generic.List<string>(Allergens?.Split(',') ?? Array.Empty<string>()),
                        CalorieGoal = calorieGoalValue,
                        HealthConditions = new System.Collections.Generic.List<string>(HealthConditions?.Split(',') ?? Array.Empty<string>())
                    }
                };

                await _authService.RegisterAsync(command);
                _navigationService.NavigateTo("Login");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Registration failed: {ex.Message}";
            }
        }

        private bool CanRegister()
        {
            return !string.IsNullOrWhiteSpace(Email) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   int.TryParse(CalorieGoal, out _);
        }

        [RelayCommand]
        private void GoBack()
        {
            _navigationService.NavigateTo("Authentication");
        }
    }
}