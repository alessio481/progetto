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
        public long TempoGuidaSecondi { get; set; }
        public long? InizioGuidaUnix { get; set; }
        public FiltriDashboardViewModel FiltriRicerca { get; set; } = new();
        public List<SchedaVeicoloViewModel> SchedeVeicoli { get; set; } = new();

        public int TotaleVeicoli
        {
            get
            {
                return SchedeVeicoli.Count;
            }
        }

        public int TotaleInUso
        {
            get
            {
                return SchedeVeicoli.Count(veicolo => veicolo.Stato == "in uso");
            }
        }

        public int TotaleManutenzione
        {
            get
            {
                return SchedeVeicoli.Count(veicolo =>
                    veicolo.Stato == "in manutenzione" ||
                    veicolo.Stato == "in richiesta manutenzione");
            }
        }
    }

    public class FiltriDashboardViewModel
    {
        public string? Ricerca { get; set; }
        public int? Gruppo { get; set; }
        public string? Stato { get; set; }
        public int? IdAssegnatario { get; set; }
        public int? LivelloCarburante { get; set; }
        public List<OpzioneSelectViewModel> OpzioniAssegnatario { get; set; } = new();

        public bool CiSonoFiltriAttivi
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Ricerca) ||
                       Gruppo.HasValue ||
                       !string.IsNullOrWhiteSpace(Stato) ||
                       IdAssegnatario.HasValue ||
                       LivelloCarburante.HasValue;
            }
        }
    }

    public class SchedaVeicoloViewModel
    {
        public int IdVeicolo { get; set; }
        public string Modello { get; set; } = string.Empty;
        public string Targa { get; set; } = string.Empty;
        public string Stato { get; set; } = "non in uso";
        public string Gruppo { get; set; } = string.Empty;
        public string? NomeAssegnatario { get; set; }
        public int Chilometraggio { get; set; }
        public int LivelloCarburante { get; set; }
        public string? TipoCarburante { get; set; }
        public string LinkImmagine { get; set; } = string.Empty;
        public DateTime? DataPossesso { get; set; }
        public DateTime? RevisioneInizio { get; set; }
        public DateTime? RevisioneScadenza { get; set; }
        public DateTime? BolloInizio { get; set; }
        public DateTime? BolloScadenza { get; set; }
        public DateTime? TagliandoInizio { get; set; }
        public DateTime? TagliandoScadenza { get; set; }
        public DateTime? AssicurazioneInizio { get; set; }
        public DateTime? AssicurazioneScadenza { get; set; }
        public bool MostraTimerGuida { get; set; }
        public long? InizioGuidaUnix { get; set; }
        public bool PuoModificare { get; set; }
        public bool PuoUsareOra { get; set; }
        public bool PuoSegnalareManutenzione { get; set; }
        public bool PuoApprovareManutenzione { get; set; }
        public bool PuoTerminareManutenzione { get; set; }
    }

    public class FormVeicoloViewModel
    {
        // Questo model serve sia per la creazione sia per la modifica.
        public int? IdVeicolo { get; set; }
        public bool EAdmin { get; set; }
        public bool ECreazione { get; set; }

        [Required(ErrorMessage = "Inserisci il modello.")]
        public string Modello { get; set; } = string.Empty;

        [Required(ErrorMessage = "Inserisci la targa.")]
        public string Targa { get; set; } = string.Empty;

        [Display(Name = "Assegnatario")]
        public int? IdAssegnatario { get; set; }

        [Range(1, 3, ErrorMessage = "Seleziona un gruppo valido.")]
        public int Gruppo { get; set; } = 1;

        [Range(0, int.MaxValue, ErrorMessage = "Inserisci un chilometraggio valido.")]
        public int Chilometraggio { get; set; }

        [Range(0, 2, ErrorMessage = "Seleziona un livello carburante valido.")]
        public int LivelloCarburante { get; set; } = 2;

        public string? TipoCarburante { get; set; }

        [Required(ErrorMessage = "Seleziona lo stato del veicolo.")]
        public string Stato { get; set; } = "non in uso";

        [DataType(DataType.Date)]
        public DateTime? DataPossesso { get; set; }

        public string? LinkImmagine { get; set; }

        [DataType(DataType.Date)]
        public DateTime? RevisioneInizio { get; set; }

        [DataType(DataType.Date)]
        public DateTime? BolloInizio { get; set; }

        [DataType(DataType.Date)]
        public DateTime? TagliandoInizio { get; set; }

        [DataType(DataType.Date)]
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

    public class FormUtenteViewModel
    {
        [Required(ErrorMessage = "Inserisci il nome.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Inserisci il cognome.")]
        public string Cognome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Inserisci l'email.")]
        [EmailAddress(ErrorMessage = "Inserisci un indirizzo email valido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Inserisci la password.")]
        [MinLength(5, ErrorMessage = "La password deve avere almeno 5 caratteri.")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Inserisci la data di nascita.")]
        public DateTime? DataNascita { get; set; }
    }

    public class TempiGuidaUtentiViewModel
    {
        public string NomeAdmin { get; set; } = string.Empty;
        public List<RigaTempoGuidaUtenteViewModel> Utenti { get; set; } = new();
    }

    public class RigaTempoGuidaUtenteViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public string Cognome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public long TempoGuidaSecondi { get; set; }
        public long? InizioGuidaUnix { get; set; }
    }
}
