using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FleetManager.Models
{
    public class Driver
    {
        // Chiave primaria per la tabella Drivers
        [Key]
        public int DriverID { get; set; }

        // FK verso Utenti (ogni Driver ?anche un Utente registrato nel sistema)
        [Required]
        public int UtenteID { get; set; }

        // Navigation property verso l'utente collegato
        public Utente Utente { get; set; }

        // Numero della patente del driver
        [Required]
        [StringLength(30)]
        public string LicenseNumber { get; set; } = string.Empty;

        // Telefono opzionale
        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        // Data di assunzione del driver
        public DateTime HireDate { get; set; } = DateTime.Now;

        // Eventuale data di scadenza patente (opzionale ma utile)
        public DateTime? LicenseExpiration { get; set; }

        // Altre note
        [StringLength(200)]
        public string? Note { get; set; }
    }
}
