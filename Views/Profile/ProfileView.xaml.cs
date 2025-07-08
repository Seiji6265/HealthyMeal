using HealthyMeal.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HealthyMeal.Views.Profile
{
    public partial class ProfileView : UserControl
    {
        public ProfileView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ProfileViewModel oldVm)
            {
                oldVm.ThemeChanged -= OnThemeChanged;
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is ProfileViewModel newVm)
            {
                Debug.WriteLine("[ProfileView] DataContext changed. Subscribing to events.");
                newVm.ThemeChanged += OnThemeChanged;
                newVm.PropertyChanged += OnViewModelPropertyChanged;

                // Update visibility immediately
                UpdateVisibility(newVm.IsEditingPreferences);

                Loaded += (s, ev) =>
                {
                    Debug.WriteLine("[ProfileView] View loaded. Applying initial theme.");
                    OnThemeChanged();
                };
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProfileViewModel.IsEditingPreferences) && sender is ProfileViewModel vm)
            {
                UpdateVisibility(vm.IsEditingPreferences);
            }
        }

        private void UpdateVisibility(bool isEditing)
        {
            ViewModePanel.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
            EditModePanel.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnThemeChanged()
        {
            Debug.WriteLine("[ProfileView] OnThemeChanged event received. Applying theme manually.");
            // ... (manual color setting logic) ...
        }
    }
}