using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace HealthyMeal.Models
{
    public class MealPlan : EntityBase
    {
        public int UserId { get; set; }

        // Cały plan (mapa dat do przepisów) przechowywany jako JSON.
        public string PlanJson { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }
    }
}