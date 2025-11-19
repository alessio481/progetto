using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class Prenotazione
    {
        public int PrenotazioneID { get; set; }

        [Required]
        [Display(Name = "Utente")]
        public int UtenteID { get; set; }

        [Required]
        [Display(Name = "Veicolo")]
        public int VeicoloID { get; set; }

        [Required]
        [Display(Name = "Data Inizio")]
        [DataType(DataType.Date)]
        public DateTime DataInizio { get; set; }

        [Required]
        [Display(Name = "Data Fine")]
        [DataType(DataType.Date)]
        public DateTime DataFine { get; set; }

        [Display(Name = "Scopo")]
        public string? Scopo { get; set; }

        [Display(Name = "Stato")]
        public string Stato { get; set; } = "InAttesa"; // InAttesa, Approvata, Rifiutata, Completata

        [Display(Name = "Data Creazione")]
        public DateTime DataCreazione { get; set; } = DateTime.Now;

        // Navigation properties
        public Utente? Utente { get; set; }
        public Veicolo? Veicolo { get; set; }
    }
}