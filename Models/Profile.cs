using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthyMeal.Models
{
    public class Profile : EntityBase
    {
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public string PreferencesJson { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
