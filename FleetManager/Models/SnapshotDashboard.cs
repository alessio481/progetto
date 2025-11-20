using System.ComponentModel.DataAnnotations;

namespace FleetManager.Models
{
    public class DashboardSnapshot
    {
        public int DashboardSnapshotID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Giorno { get; set; }

        // DPR: dati riepilogativi
        public int VeicoliTotali { get; set; }
        public int VeicoliDisponibili { get; set; }
        public int VeicoliInUso { get; set; }
        public int VeicoliInManutenzione { get; set; }
        public int SegnalazioniAperte { get; set; }

        // opzionale per espansioni future
        public int PrenotazioniAttive { get; set; }
        public int ManutenzioniAperte { get; set; }
    }
}
