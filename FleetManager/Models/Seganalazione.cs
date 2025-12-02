using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class Segnalazione
    {
        public int SegnalazioneID { get; set; }

        [Required]
        [Display(Name = "Utente")]
        public int UtenteID { get; set; }

        [Display(Name = "Veicolo")]
        public int? VeicoloID { get; set; }

        public Utente? Utente { get; set; }
        public Veicolo? Veicolo { get; set; }

        [Display(Name = "Posto Auto")]
        public int? PostoID { get; set; } //da fare segnalazione.PostoID = veicolo.PostoAuto; quando si segnala

        [Required]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; } = string.Empty; // Problema meccanico, Incidente, Problema parcheggio, etc.

        [Display(Name = "Descrizione")]
        public string? Descrizione { get; set; }

        [Display(Name = "Stato")]
        public string Stato { get; set; } = "Aperta"; // Aperta, InLavorazione, Risolta, Chiusa

        [Display(Name = "Data Creazione")]
        public DateTime DataCreazione { get; set; } = DateTime.Now;

    }
}