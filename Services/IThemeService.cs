namespace HealthyMeal.Services
{
    public enum Theme
    {
        Light,
        Dark
    }

    public interface IThemeService
    {
        Theme CurrentTheme { get; }
        void SetTheme(Theme theme);
    }
}