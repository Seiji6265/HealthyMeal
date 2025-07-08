using System.Windows.Controls;

namespace HealthyMeal.Views.Auth
{
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();
            PasswordBox.PasswordChanged += OnPasswordChanged;
        }

        private void OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.RegisterViewModel viewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}