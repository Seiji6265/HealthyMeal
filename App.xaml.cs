using HealthyMeal.Services;
using HealthyMeal.ViewModels;
using HealthyMeal.Views.Auth;
using HealthyMeal.Views.Dashboard;
using HealthyMeal.Views.MealPlan;
using HealthyMeal.Views.Recipes;
using HealthyMeal.Views.Profile;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using SQLitePCL;

namespace HealthyMeal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public App()
        {
            // This line MUST be executed before any other database code.
            // It explicitly tells the application to use the SQLCipher provider.
            Batteries_V2.Init();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register HTTP client for AI service
            services.AddHttpClient<GeminiAIService>();

            // Register services
            services.AddSingleton<DatabaseService>();
            services.AddTransient<IPasswordHasher, PasswordHasherService>();
            services.AddTransient<IProfileRepository, ProfileRepository>();
            services.AddTransient<IRecipeRepository, RecipeRepository>();
            services.AddTransient<IMealPlanRepository, MealPlanRepository>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IAIService, GeminiAIService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IApplicationState, ApplicationState>();
            services.AddSingleton<IThemeService, ThemeService>();

            // Register encryption and API key services
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddTransient<IApiKeyService, ApiKeyService>();

            // Register ViewModels
            services.AddTransient<AuthenticationViewModel>();
            services.AddTransient<RegisterViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<RecipeListViewModel>();
            services.AddTransient<RecipeDetailViewModel>();
            services.AddTransient<RecipeEditorViewModel>();
            services.AddTransient<MealPlanViewModel>();
            services.AddTransient<ProfileViewModel>();
            services.AddTransient<SettingsViewModel>();

            // Register Views (as transient so a new one is created each time)
            services.AddTransient<AuthenticationView>();
            services.AddTransient<RegisterView>();
            services.AddTransient<LoginView>();
            services.AddTransient<DashboardView>();
            services.AddTransient<RecipeListView>();
            services.AddTransient<RecipeDetailView>();
            services.AddTransient<RecipeEditorView>();
            services.AddTransient<MealPlanView>();
            services.AddTransient<ProfileView>();

            // Register the main window
            services.AddSingleton<MainWindow>();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var themeService = ServiceProvider.GetRequiredService<IThemeService>();
            themeService.SetTheme(Theme.Light);

            var authService = ServiceProvider.GetRequiredService<IAuthService>();
            var navigationService = ServiceProvider.GetRequiredService<INavigationService>();

            if (await authService.TryAutoLoginAsync())
            {
                navigationService.NavigateTo("Dashboard");
            }
            else
            {
                navigationService.NavigateTo("Authentication");
            }

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}
