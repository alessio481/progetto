using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class Veicolo
    {
        public int VeicoloId { get; set; }

        [Required]
        [StringLength(10)]
        public string Targa { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Modello { get; set; } = string.Empty;

        [StringLength(20)]
        public string Tipo { get; set; } = "Auto";

        [StringLength(20)]
        public string Stato { get; set; } = "Disponibile";

        // 0 = riserva, 1 = medio, 2 = alto.
        [Range(0, 2)]
        public int LivelloCarburante { get; set; } = 2;

        public int Chilometraggio { get; set; }
        public string? Carburante { get; set; }
        public int Gruppo { get; set; } = 1;
        public string? ImageUrl { get; set; }
        public DateTime? DataPossesso { get; set; }

        // Salviamo solo la data iniziale. La scadenza si calcola nel controller.
        public DateTime? RevisioneInizio { get; set; }
        public DateTime? BolloInizio { get; set; }
        public DateTime? TagliandoInizio { get; set; }
        public DateTime? AssicurazioneInizio { get; set; }

        public DateTime DataCreazione { get; set; } = DateTime.Now;
        public DateTime? DataAggiornamento { get; set; }

        // Se valorizzato, indica chi ha in carico il veicolo.
        public int? UtentePrenotatoID { get; set; }
        public Utente? UtentePrenotato { get; set; }
    }
}
