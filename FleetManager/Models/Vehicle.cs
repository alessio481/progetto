using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public enum VehicleStatus
    {
        Disponibile,
        Assegnato,
        Manutenzione,
        Dismesso
    }

    public class Vehicle
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        public string Targa { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Modello { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Tipo { get; set; } = "Auto";

        [Display(Name = "Data immatricolazione"), DataType(DataType.Date)]
        public DateTime DataImmatricolazione { get; set; } = DateTime.Today;

        [Display(Name = "Stato"), Required]
        public VehicleStatus Stato { get; set; } = VehicleStatus.Disponibile;

        [Display(Name = "Chilometri attuali")]
        public int KmAttuali { get; set; }

        [StringLength(200)]
        public string? Note { get; set; }

        public ICollection<Assignment> Assegnazioni { get; set; } = new List<Assignment>();
    }
}
