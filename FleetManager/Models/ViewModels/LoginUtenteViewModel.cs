using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models.ViewModels
{
    public class LoginUtenteViewModel
    {
        // ViewModel molto semplice per la pagina di login.
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
