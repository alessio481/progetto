using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class Manutenzione
    {
        public int ManutenzioneID { get; set; }

        [Required]
        [Display(Name = "Veicolo")]
        public int VeicoloID { get; set; }

        [Required]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } = string.Empty; // Cambio olio, Revisione, Sostituzione pneumatici, etc.

        [Display(Name = "Data Programmata")]
        [DataType(DataType.Date)]
        public DateTime? DataProgrammata { get; set; }

        [Display(Name = "Data Completamento")]
        [DataType(DataType.Date)]
        public DateTime? DataCompletamento { get; set; }

        [Display(Name = "Descrizione")]
        public string? Descrizione { get; set; }

        [Display(Name = "Stato")]
        public string Stato { get; set; } = "Pianificata"; // Pianificata, InCorso, Completata, Annullata

        [Display(Name = "Data Creazione")]
        public DateTime DataCreazione { get; set; } = DateTime.Now;

        // Navigation properties
        public Veicolo? Veicolo { get; set; }
    }
}