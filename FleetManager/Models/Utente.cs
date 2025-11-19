using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class Utente
    {
        public int UtenteID { get; set; }

        [Required]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Cognome")]
        public string Cognome { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Data di Nascita")]
        [DataType(DataType.Date)]
        public DateTime DataNascita { get; set; }

        [Required]
        [Display(Name = "Ruolo")]
        public string Ruolo { get; set; } = "User"; // Admin, User, Driver

        [Display(Name = "Data Registrazione")]
        public DateTime DataRegistrazione { get; set; } = DateTime.Now;

        [Display(Name = "Attivo")]
        public bool Attivo { get; set; } = true;

        [Display(Name = "Ultimo Accesso")]
        public DateTime? UltimoAccesso { get; set; }

        // Navigation properties - 添加这些以匹配DbContext配置
        public ICollection<Prenotazione> Prenotazioni { get; set; } = new List<Prenotazione>();
        public ICollection<Segnalazione> Segnalazioni { get; set; } = new List<Segnalazione>();

        // Computed property per nome completo
        [Display(Name = "Nome Completo")]
        public string NomeCompleto => $"{Nome} {Cognome}";
    }
}