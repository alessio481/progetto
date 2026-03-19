using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class Veicolo
    {
        public enum StatoVeicolo
        {
            FuoriServizio,
            Disponibile,
            InUso,
            Manutenzione
        }

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

        [Range(0, 100)]
        public int LivelloCarburante { get; set; } = 100;
        public int Chilometraggio { get; set; }

        public string? Colore { get; set; }
        public string? Carburante { get; set; }
        public int? Cilindrata { get; set; }
        public string? Gruppo { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? DataPossesso { get; set; }
        public DateTime? RevisioneInizio { get; set; }
        public DateTime? RevisioneScadenza { get; set; }
        public DateTime? BolloInizio { get; set; }
        public DateTime? BolloScadenza { get; set; }
        public DateTime? TagliandoInizio { get; set; }
        public DateTime? TagliandoScadenza { get; set; }
        public DateTime? AssicurazioneInizio { get; set; }
        public DateTime? AssicurazioneScadenza { get; set; }

        public DateTime DataCreazione { get; set; } = DateTime.Now;
        public DateTime? DataAggiornamento { get; set; }

        public int? UtentePrenotatoID { get; set; }
        public Utente? UtentePrenotato { get; set; }
        public List<Prenotazione>? Prenotazioni { get; set; }

        public StatoVeicolo GetStatoEnum()
        {
            return Stato switch
            {
                "FuoriServizio" => StatoVeicolo.FuoriServizio,
                "Disponibile" => StatoVeicolo.Disponibile,
                "InUso" => StatoVeicolo.InUso,
                "Manutenzione" => StatoVeicolo.Manutenzione,
                _ => StatoVeicolo.Disponibile
            };
        }

        public void SetStatoEnum(StatoVeicolo status)
        {
            Stato = status.ToString();
        }
    }
}
