namespace HealthyMeal.Models
{
    /// <summary>
    /// Represents the currently authenticated user in the application.
    /// This is a lightweight object to hold session data.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
    }
}