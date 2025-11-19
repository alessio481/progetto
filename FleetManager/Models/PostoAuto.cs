using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class PostoAuto
    {
        public int PostoID { get; set; }

        [Required]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [Display(Name = "Zona")]
        public string? Zona { get; set; }

        [Display(Name = "Stato")]
        public string Stato { get; set; } = "Libero"; // Libero, Occupato, Manutenzione
    }
}