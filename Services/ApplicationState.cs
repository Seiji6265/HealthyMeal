using HealthyMeal.Models;

namespace HealthyMeal.Services
{
    public class ApplicationState : IApplicationState
    {
        public User? CurrentUser { get; set; }
    }
}