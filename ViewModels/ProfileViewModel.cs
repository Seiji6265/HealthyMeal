using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.Services;
using HealthyMeal.DTOs;
using HealthyMeal.Models;
using System.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HealthyMeal.ViewModels
{
    public partial class ProfileViewModel : ViewModelBase
    {
        private readonly IThemeService _themeService;
        private readonly IApplicationState _appState;
        private readonly INavigationService _navigationService;
        private readonly IProfileRepository _profileRepository;

        public event Action? ThemeChanged;

        [ObservableProperty]
        private string _userEmail;

        [ObservableProperty]
        private bool _isDarkMode;

        // User Preferences Properties
        [ObservableProperty]
        private string _allergens = string.Empty;

        [ObservableProperty]
        private int _calorieGoal = 2000;

        [ObservableProperty]
        private string _healthConditions = string.Empty;

        [ObservableProperty]
        private string _additionalPreferences = string.Empty;

        [ObservableProperty]
        private bool _isEditingPreferences;

        [ObservableProperty]
        private bool _isSavingPreferences;

        public ProfileViewModel(IThemeService themeService, IApplicationState appState, INavigationService navigationService, IProfileRepository profileRepository)
        {
            _themeService = themeService;
            _appState = appState;
            _navigationService = navigationService;
            _profileRepository = profileRepository;

            UserEmail = _appState.CurrentUser?.Email ?? "N/A";
            IsDarkMode = _themeService.CurrentTheme == Theme.Dark;

            // Initialize preferences
            _ = LoadUserPreferences();
        }

        partial void OnIsDarkModeChanged(bool value)
        {
            _themeService.SetTheme(value ? Theme.Dark : Theme.Light);
            ThemeChanged?.Invoke();
        }

        [RelayCommand]
        private void DeleteAccount()
        {
            var result = MessageBox.Show("Are you sure you want to permanently delete your account and all data? This action cannot be undone.", "Delete Account", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                // TODO: Implement account deletion logic in a service
                MessageBox.Show("Account deleted successfully.");
                _navigationService.NavigateTo("Authentication");
            }
        }

        [RelayCommand]
        private void GoToDashboard()
        {
            _navigationService.NavigateTo("Dashboard");
        }

        [RelayCommand]
        private void LogOut()
        {
            var result = MessageBox.Show("Czy na pewno chcesz się wylogować?",
                                       "Wylogowanie",
                                       MessageBoxButton.YesNo,
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear current user
                _appState.CurrentUser = null;

                // Navigate to authentication view
                _navigationService.NavigateTo("Authentication");

                MessageBox.Show("Zostałeś pomyślnie wylogowany.",
                              "Wylogowanie",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private void EditPreferences()
        {
            IsEditingPreferences = true;
        }

        [RelayCommand]
        private void CancelEditPreferences()
        {
            IsEditingPreferences = false;
            // Reload original preferences
            _ = LoadUserPreferences();
        }

        [RelayCommand]
        private async Task SavePreferences()
        {
            if (_appState.CurrentUser == null)
            {
                MessageBox.Show("Błąd: Brak zalogowanego użytkownika.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsSavingPreferences = true;

            try
            {
                // Parse allergens and health conditions from text
                var allergensList = Allergens.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                            .Select(x => x.Trim())
                                            .Where(x => !string.IsNullOrEmpty(x))
                                            .ToList();

                var healthConditionsList = HealthConditions.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(x => x.Trim())
                                                          .Where(x => !string.IsNullOrEmpty(x))
                                                          .ToList();

                // Create updated preferences DTO
                var preferences = new UserPreferencesDto
                {
                    Allergens = allergensList,
                    CalorieGoal = CalorieGoal,
                    HealthConditions = healthConditionsList,
                    AdditionalPreferences = AdditionalPreferences ?? string.Empty
                };

                // Get current profile
                var profile = await _profileRepository.GetByIdAsync(_appState.CurrentUser.Id);
                if (profile == null)
                {
                    MessageBox.Show("Błąd: Nie znaleziono profilu użytkownika.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Update preferences JSON
                profile.PreferencesJson = JsonSerializer.Serialize(preferences);

                // Save to database
                await _profileRepository.UpdateAsync(profile);

                IsEditingPreferences = false;
                MessageBox.Show("Preferencje zostały pomyślnie zaktualizowane!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas zapisywania preferencji: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSavingPreferences = false;
            }
        }

        private async Task LoadUserPreferences()
        {
            System.Diagnostics.Debug.WriteLine("[ProfileViewModel] LoadUserPreferences started");

            if (_appState.CurrentUser == null)
            {
                System.Diagnostics.Debug.WriteLine("[ProfileViewModel] No current user found");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Loading preferences for user ID: {_appState.CurrentUser.Id}");

            try
            {
                var profile = await _profileRepository.GetByIdAsync(_appState.CurrentUser.Id);
                if (profile == null || string.IsNullOrEmpty(profile.PreferencesJson))
                {
                    System.Diagnostics.Debug.WriteLine("[ProfileViewModel] No profile or empty preferences JSON - setting defaults");
                    // Set default values
                    Allergens = "Brak alergenów";
                    CalorieGoal = 2000;
                    HealthConditions = "Brak ograniczeń";
                    AdditionalPreferences = "Brak dodatkowych preferencji";
                    System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Set defaults: Allergens='{Allergens}', CalorieGoal={CalorieGoal}");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Found preferences JSON: {profile.PreferencesJson}");

                var preferences = JsonSerializer.Deserialize<UserPreferencesDto>(profile.PreferencesJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (preferences != null)
                {
                    Allergens = preferences.Allergens.Any() ? string.Join("\n", preferences.Allergens) : "Brak alergenów";
                    CalorieGoal = preferences.CalorieGoal;
                    HealthConditions = preferences.HealthConditions.Any() ? string.Join("\n", preferences.HealthConditions) : "Brak ograniczeń";
                    AdditionalPreferences = !string.IsNullOrEmpty(preferences.AdditionalPreferences) ? preferences.AdditionalPreferences : "Brak dodatkowych preferencji";

                    System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Loaded preferences: Allergens='{Allergens}', CalorieGoal={CalorieGoal}, HealthConditions='{HealthConditions}', Additional='{AdditionalPreferences}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ProfileViewModel] Failed to deserialize preferences - setting defaults");
                    Allergens = "Brak alergenów";
                    CalorieGoal = 2000;
                    HealthConditions = "Brak ograniczeń";
                    AdditionalPreferences = "Brak dodatkowych preferencji";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Error loading preferences: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ProfileViewModel] Stack trace: {ex.StackTrace}");
                // Set default values on error
                Allergens = "Brak alergenów";
                CalorieGoal = 2000;
                HealthConditions = "Brak ograniczeń";
                AdditionalPreferences = "Brak dodatkowych preferencji";
            }

            System.Diagnostics.Debug.WriteLine("[ProfileViewModel] LoadUserPreferences completed");
        }
    }
}