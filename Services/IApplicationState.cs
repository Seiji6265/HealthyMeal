using HealthyMeal.Models;

namespace HealthyMeal.Services
{
    public interface IApplicationState
    {
        User? CurrentUser { get; set; }
    }
}