using System.ComponentModel.DataAnnotations;
using FleetManager.Models;
namespace FleetManager.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(50)]
        public string LicensePlate { get; set; } = string.Empty;

        [Required]
        public VehicleStatus Status { get; set; }

        public DateTime LastMaintenanceDate { get; set; }

        public override string ToString()
        {
            return $"{Year} {Make} {Model} - {LicensePlate} ({Status})";
        }
    }
}   
