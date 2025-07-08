using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthyMeal.Models
{
    public class Recipe : EntityBase
    {
        // Null oznacza przepis systemowy (bazowy).
        public int? OwnerId { get; set; }

        public bool IsCustom { get; set; }

        public string Name { get; set; } = null!;

        public int? PrepTimeMinutes { get; set; }

        // Szczegółowe dane (składniki, kroki, wartości odżywcze) jako JSON.
        public string DataJson { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}