using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthyMeal.Services;

namespace HealthyMeal.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly IThemeService _themeService;
        private readonly IApiKeyService _apiKeyService;
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private bool _isDarkMode;

        [ObservableProperty]
        private string _geminiApiKey = string.Empty;

        [ObservableProperty]
        private bool _isApiKeyVisible = false;

        [ObservableProperty]
        private bool _isSaving = false;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public SettingsViewModel(IThemeService themeService, IApiKeyService apiKeyService, INavigationService navigationService)
        {
            _themeService = themeService;
            _apiKeyService = apiKeyService;
            _navigationService = navigationService;
            IsDarkMode = _themeService.CurrentTheme == Theme.Dark;

            // Load current API key on startup
            _ = LoadApiKeyAsync();
        }

        public string ApiKeyDisplayText
        {
            get
            {
                if (IsApiKeyVisible || string.IsNullOrEmpty(GeminiApiKey))
                    return GeminiApiKey;

                // Show first 6 and last 4 characters, mask the middle
                if (GeminiApiKey.Length > 10)
                    return GeminiApiKey.Substring(0, 6) + "..." + GeminiApiKey.Substring(GeminiApiKey.Length - 4);

                return new string('*', GeminiApiKey.Length);
            }
        }

        partial void OnIsDarkModeChanged(bool value)
        {
            _themeService.SetTheme(value ? Theme.Dark : Theme.Light);
        }

        partial void OnGeminiApiKeyChanged(string value)
        {
            StatusMessage = string.Empty; // Clear status when user types
            OnPropertyChanged(nameof(ApiKeyDisplayText));
        }

        partial void OnIsApiKeyVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(ApiKeyDisplayText));
        }

        [RelayCommand]
        private void ToggleApiKeyVisibility()
        {
            IsApiKeyVisible = !IsApiKeyVisible;
        }

        [RelayCommand]
        private async Task SaveApiKey()
        {
            if (string.IsNullOrWhiteSpace(GeminiApiKey))
            {
                StatusMessage = "Klucz API nie może być pusty.";
                return;
            }

            IsSaving = true;
            StatusMessage = "Zapisywanie klucza...";

            try
            {
                await _apiKeyService.SetGeminiApiKeyAsync(GeminiApiKey);
                StatusMessage = "Klucz API został pomyślnie zapisany i zaszyfrowany.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Błąd zapisu: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
            }
        }

        [RelayCommand]
        private void GoToDashboard()
        {
            _navigationService.NavigateTo("Dashboard");
        }

        private async Task LoadApiKeyAsync()
        {
            var key = await _apiKeyService.GetGeminiApiKeyAsync();
            if (!string.IsNullOrEmpty(key))
            {
                GeminiApiKey = key;
                StatusMessage = "Klucz API załadowany z bazy danych.";
            }
            else
            {
                StatusMessage = "Wprowadź swój klucz API Gemini.";
            }
        }
    }
}