namespace HealthyMeal.Services
{
    public interface INavigationService
    {
        void NavigateTo(string viewName, object? parameter = null);
    }
}