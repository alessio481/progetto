using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models.ViewModels
{
    public class DashboardIndexViewModel
    {
        // Un solo oggetto contiene tutto quello che serve alla pagina dashboard.
        public bool IsAdmin { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
        public string? StatusMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string ReturnUrl { get; set; } = "/Dashboard";
        public DashboardFiltersViewModel Filters { get; set; } = new();
        public List<DashboardCarCardViewModel> Cars { get; set; } = new();

        public int TotalCount => Cars.Count;
        public int InUseCount => Cars.Count(car => car.Stato == "in uso");
        public int MaintenanceCount => Cars.Count(car => car.Stato.Contains("manutenzione"));
    }

    public class DashboardFiltersViewModel
    {
        public string? Search { get; set; }
        public string? Group { get; set; }
        public string? Status { get; set; }
        public string? Owner { get; set; }
        public int? FuelLevel { get; set; }
    }

    public class DashboardCarCardViewModel
    {
        public int Id { get; set; }
        public string Modello { get; set; } = string.Empty;
        public string Targa { get; set; } = string.Empty;
        public string Stato { get; set; } = "non in uso";
        public string Gruppo { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public int? OwnerId { get; set; }
        public int Chilometraggio { get; set; }
        public int FuelLevel { get; set; }
        public string? FuelType { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime? DataPossesso { get; set; }
        public DateTime? RevisioneInizio { get; set; }
        public DateTime? RevisioneScadenza { get; set; }
        public DateTime? BolloInizio { get; set; }
        public DateTime? BolloScadenza { get; set; }
        public DateTime? TagliandoInizio { get; set; }
        public DateTime? TagliandoScadenza { get; set; }
        public DateTime? AssicurazioneInizio { get; set; }
        public DateTime? AssicurazioneScadenza { get; set; }
        public bool CanEdit { get; set; }
        public bool CanUseNow { get; set; }
        public bool CanRequestMaintenance { get; set; }
        public bool CanApproveMaintenance { get; set; }
    }

    public class DashboardCarFormViewModel
    {
        // Questo model serve sia per creare sia per modificare un veicolo.
        public int? Id { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsCreate { get; set; }
        public string ReturnUrl { get; set; } = "/Dashboard";

        [Required]
        public string Modello { get; set; } = string.Empty;

        [Required]
        public string Targa { get; set; } = string.Empty;

        [Display(Name = "Assegnatario")]
        public int? OwnerId { get; set; }

        public string Gruppo { get; set; } = "FONDAZIONE SETTORE-1";

        [Range(0, int.MaxValue)]
        public int Chilometraggio { get; set; }

        [Range(0, 2)]
        public int FuelLevel { get; set; } = 2;

        public string? FuelType { get; set; }
        public string Stato { get; set; } = "non in uso";
        public DateTime? DataPossesso { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? RevisioneInizio { get; set; }
        public DateTime? BolloInizio { get; set; }
        public DateTime? TagliandoInizio { get; set; }
        public DateTime? AssicurazioneInizio { get; set; }

        public List<SelectItemViewModel> OwnerOptions { get; set; } = new();
        public List<SelectItemViewModel> GroupOptions { get; set; } = new();
        public List<SelectItemViewModel> FuelOptions { get; set; } = new();
        public List<SelectItemViewModel> StatusOptions { get; set; } = new();
    }

    public class SelectItemViewModel
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
