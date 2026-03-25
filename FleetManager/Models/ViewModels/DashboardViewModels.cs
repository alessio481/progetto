using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models.ViewModels
{
    public class DashboardPaginaViewModel
    {
        // Questa classe contiene tutto quello che serve alla pagina dashboard.
        public bool EAdmin { get; set; }
        public string NomeUtenteCorrente { get; set; } = string.Empty;
        public string? MessaggioOperazione { get; set; }
        public string? MessaggioErrore { get; set; }
        public string UrlRitorno { get; set; } = "/Dashboard";
        public FiltriDashboardViewModel Filtri { get; set; } = new();
        public List<SchedaVeicoloViewModel> Veicoli { get; set; } = new();

        public int TotaleVeicoli => Veicoli.Count;
        public int TotaleInUso => Veicoli.Count(veicolo => veicolo.Stato == "in uso");
        public int TotaleManutenzione => Veicoli.Count(veicolo => veicolo.Stato.Contains("manutenzione"));
    }

    public class FiltriDashboardViewModel
    {
        public string? Ricerca { get; set; }
        public string? Gruppo { get; set; }
        public string? Stato { get; set; }
        public string? Assegnatario { get; set; }
        public int? LivelloCarburante { get; set; }

        public bool CiSonoFiltriAttivi =>
            !string.IsNullOrWhiteSpace(Ricerca) ||
            !string.IsNullOrWhiteSpace(Gruppo) ||
            !string.IsNullOrWhiteSpace(Stato) ||
            !string.IsNullOrWhiteSpace(Assegnatario) ||
            LivelloCarburante.HasValue;
    }

    public class SchedaVeicoloViewModel
    {
        public int Id { get; set; }
        public string Modello { get; set; } = string.Empty;
        public string Targa { get; set; } = string.Empty;
        public string Stato { get; set; } = "non in uso";
        public string Gruppo { get; set; } = string.Empty;
        public string? NomeAssegnatario { get; set; }
        public int? IdAssegnatario { get; set; }
        public int Chilometraggio { get; set; }
        public int LivelloCarburante { get; set; }
        public string? TipoCarburante { get; set; }
        public string UrlImmagine { get; set; } = string.Empty;
        public DateTime? DataPossesso { get; set; }
        public DateTime? RevisioneInizio { get; set; }
        public DateTime? RevisioneScadenza { get; set; }
        public DateTime? BolloInizio { get; set; }
        public DateTime? BolloScadenza { get; set; }
        public DateTime? TagliandoInizio { get; set; }
        public DateTime? TagliandoScadenza { get; set; }
        public DateTime? AssicurazioneInizio { get; set; }
        public DateTime? AssicurazioneScadenza { get; set; }
        public bool PuoModificare { get; set; }
        public bool PuoUsareOra { get; set; }
        public bool PuoSegnalareManutenzione { get; set; }
        public bool PuoApprovareManutenzione { get; set; }
    }

    public class FormVeicoloViewModel
    {
        // Questo model serve sia per la creazione sia per la modifica.
        public int? Id { get; set; }
        public bool EAdmin { get; set; }
        public bool ECreazione { get; set; }
        public string UrlRitorno { get; set; } = "/Dashboard";

        [Required]
        public string Modello { get; set; } = string.Empty;

        [Required]
        public string Targa { get; set; } = string.Empty;

        [Display(Name = "Assegnatario")]
        public int? IdAssegnatario { get; set; }

        public string Gruppo { get; set; } = "FONDAZIONE SETTORE-1";

        [Range(0, int.MaxValue)]
        public int Chilometraggio { get; set; }

        [Range(0, 2)]
        public int LivelloCarburante { get; set; } = 2;

        public string? TipoCarburante { get; set; }
        public string Stato { get; set; } = "non in uso";
        public DateTime? DataPossesso { get; set; }
        public string? UrlImmagine { get; set; }
        public DateTime? RevisioneInizio { get; set; }
        public DateTime? BolloInizio { get; set; }
        public DateTime? TagliandoInizio { get; set; }
        public DateTime? AssicurazioneInizio { get; set; }

        public List<OpzioneSelectViewModel> OpzioniAssegnatario { get; set; } = new();
        public List<OpzioneSelectViewModel> OpzioniGruppo { get; set; } = new();
        public List<OpzioneSelectViewModel> OpzioniCarburante { get; set; } = new();
        public List<OpzioneSelectViewModel> OpzioniStato { get; set; } = new();
    }

    public class OpzioneSelectViewModel
    {
        public string Valore { get; set; } = string.Empty;
        public string Testo { get; set; } = string.Empty;
    }
}
