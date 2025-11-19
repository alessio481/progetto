using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class Driver
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Nome { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string Cognome { get; set; } = string.Empty;

        [Required, StringLength(30)]
        public string NumeroPatente { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        public string? Telefono { get; set; }

        [StringLength(200)]
        public string? Note { get; set; }

        public ICollection<Assignment> Assegnazioni { get; set; } = new List<Assignment>();
    }
}
