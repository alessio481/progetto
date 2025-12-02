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

        public string? Colore { get; set; }
        public string? Carburante { get; set; }
        public int? Cilindrata { get; set; }

        public DateTime DataCreazione { get; set; } = DateTime.Now;
        public DateTime? DataAggiornamento { get; set; }

        // 🔧 FOREIGN KEY + Navigation Property
        public int? UtentePrenotatoID { get; set; }
        public Utente? UtentePrenotato { get; set; }

        public int? UtenteManutentoreID { get; set; }
        public Utente? UtenteManutentore { get; set; }

        // Stato attuale del parcheggio
        public int? PostoAuto { get; set; }

        // ⭐ Relazioni storiche (navigation properties)
        public List<Prenotazione>? Prenotazioni { get; set; }
        public List<Segnalazione>? Segnalazioni { get; set; }
        public List<Manutenzione>? Manutenzioni { get; set; }

        // 🎯 Conversione string <-> enum
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
