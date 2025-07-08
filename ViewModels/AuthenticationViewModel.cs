using CommunityToolkit.Mvvm.Input;
using HealthyMeal.Services;

namespace HealthyMeal.ViewModels
{
    public partial class AuthenticationViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public AuthenticationViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        private void NavigateToLogin()
        {
            _navigationService.NavigateTo("Login");
        }

        [RelayCommand]
        private void NavigateToRegister()
        {
            _navigationService.NavigateTo("Register");
        }
    }
}