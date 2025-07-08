using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.Services;
using System;
using System.Threading.Tasks;

namespace HealthyMeal.ViewModels
{
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IApplicationState _appState;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        private string _email;

        [ObservableProperty]
        private string _errorMessage;

        [ObservableProperty]
        private bool _isRememberMeEnabled;

        public LoginViewModel(IAuthService authService, INavigationService navigationService, IApplicationState appState)
        {
            _authService = authService;
            _navigationService = navigationService;
            _appState = appState;
            _email = string.Empty;
            _errorMessage = string.Empty;
        }

        [RelayCommand]
        private async Task Login(object parameter)
        {
            if (parameter is not System.Windows.Controls.PasswordBox passwordBox) return;
            var password = passwordBox.Password;

            ErrorMessage = string.Empty;

            try
            {
                var user = await _authService.LoginAsync(Email, password, IsRememberMeEnabled);

                // Set the current user via the application state service
                _appState.CurrentUser = user;

                // Navigate to the dashboard
                _navigationService.NavigateTo("Dashboard");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
        }

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Email);
        }

        [RelayCommand]
        private void GoBack()
        {
            _navigationService.NavigateTo("Authentication");
        }
    }
}