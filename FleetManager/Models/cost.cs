using System.ComponentModel.DataAnnotations;    
using System.ComponentModel.DataAnnotations.Schema; 

namespace FleetManager.Models
{
    public class Cost
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime IncurredDate { get; set; } = DateTime.Now;

        public int? VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        public int? MaintenanceId { get; set; }
        public Maintenance? Maintenance { get; set; }
    }
}   