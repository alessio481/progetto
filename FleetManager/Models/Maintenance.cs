using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetManager.Models
{
    public class Maintenance
    {
        public int Id { get; set; }
        
        // Foreign Key
        public int VehicleId { get; set; }
        
        [Required]
        public MaintenanceType MaintenanceType { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }
        
        public int Mileage { get; set; }
        
        [StringLength(100)]
        public string? Workshop { get; set; }
        
        // Navigation property
        public Vehicle Vehicle { get; set; } = null!;
    }
}
