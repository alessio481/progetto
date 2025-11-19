using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetManager.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        [Required, Display(Name = "Autista")]
        public int DriverId { get; set; }

        [Required, Display(Name = "Veicolo")]
        public int VehicleId { get; set; }

        [Display(Name = "Data inizio"), DataType(DataType.Date)]
        public DateTime DataInizio { get; set; } = DateTime.Today;

        [Display(Name = "Data fine prevista"), DataType(DataType.Date)]
        public DateTime? DataFinePrevista { get; set; }

        [Display(Name = "Data fine effettiva"), DataType(DataType.Date)]
        public DateTime? DataFineEffettiva { get; set; }

        [StringLength(200)]
        public string? Note { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public Vehicle? Vehicle { get; set; }

        [ForeignKey(nameof(DriverId))]
        public Driver? Driver { get; set; }

        public bool InCorso => !DataFineEffettiva.HasValue;
    }
}
