using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class Veicolo
    {
        public enum VehicleStatus
        {
            FuoriServizio,
            Disponibile,
            InUso,
            Manutenzione
        }

        public int VeicoloId { get; set; }

        [Required(ErrorMessage = "La targa è obbligatoria")]
        [Display(Name = "Targa")]
        [StringLength(10)]
        public string Targa { get; set; } = string.Empty;

        [Required(ErrorMessage = "La marca è obbligatoria")]
        [Display(Name = "Marca")]
        [StringLength(50)]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "Il modello è obbligatorio")]
        [Display(Name = "Modello")]
        [StringLength(50)]
        public string Modello { get; set; } = string.Empty;

        [Display(Name = "Tipo")]
        [StringLength(20)]
        public string Tipo { get; set; } = "Auto"; // Auto, Furgone etc

        [Display(Name = "Stato")]
        [StringLength(20)]
        public string Stato { get; set; } = "Disponibile";

        [Display(Name = "Data Registrazione")]
        [DataType(DataType.Date)]
        public DateTime DataRegistrazione { get; set; } = DateTime.Now;

        [Display(Name = "Livello Carburante")]
        [Range(0, 100)]
        public int LivelloCarburante { get; set; } = 100;

        // Aggiungere queste proprietà per compatibilità con DbContext
        [Display(Name = "Anno Immatricolazione")]
        public int AnnoImmatricolazione { get; set; } = DateTime.Now.Year;

        [Display(Name = "Colore")]
        [StringLength(30)]
        public string? Colore { get; set; }

        [Display(Name = "Carburante")]
        [StringLength(20)]
        public string? Carburante { get; set; }

        [Display(Name = "Cilindrata")]
        public int? Cilindrata { get; set; }

        [Display(Name = "Data Creazione")]
        public DateTime DataCreazione { get; set; } = DateTime.Now;

        [Display(Name = "Data Aggiornamento")]
        public DateTime? DataAggiornamento { get; set; }

        // FK verso PostiAuto
        public int? PostoID { get; set; }

        // Navigation properties
        public PostoAuto? PostoAuto { get; set; }
        public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
        public ICollection<Manutenzione> Manutenzioni { get; set; } = new List<Manutenzione>();
        public ICollection<Segnalazione> Segnalazioni { get; set; } = new List<Segnalazione>();

        // Metodo per convertire stringa stato in enum
        public VehicleStatus GetStatoEnum()
        {
            return Stato switch
            {
                "FuoriServizio" => VehicleStatus.FuoriServizio,
                "Disponibile" => VehicleStatus.Disponibile,
                "InUso" => VehicleStatus.InUso,
                "Manutenzione" => VehicleStatus.Manutenzione,
                _ => VehicleStatus.Disponibile
            };
        }

        // Metodo per impostare stato da enum
        public void SetStatoEnum(VehicleStatus status)
        {
            Stato = status.ToString();
        }
    }
}