using System.ComponentModel.DataAnnotations;    

namespace FleetManager.Models
{
    public class Assignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DriverId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public DateTime AssignmentDate { get; set; } = DateTime.Now;

        public DateTime? ReturnDate { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        public Vehicle vehicle {get;set;} = null!;
        public Driver Driver {get;set;} = null!;
        
    }
}   