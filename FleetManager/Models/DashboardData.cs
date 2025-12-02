namespace FleetManager.Models
{
    public class DashboardData
    {
        // Dati riepilogativi della flotta vuoto perché è un progetto vuoto
        public int VeicoliTotali { get; set; } = 0;           // numero totale di veicoli
        public int VeicoliDisponibili { get; set; } = 0;      // veicoli disponibili
        public int VeicoliInUso { get; set; } = 0;            // veicoli in uso
        public int VeicoliInManutenzione { get; set; } = 0;   // veicoli in manutenzione

        // lista vuoto di manutenzioni recenti
        public List<ManutenzioneRecente> ManutenzioniRecenti { get; set; } = new List<ManutenzioneRecente>();
    }

    public class ManutenzioneRecente
    {
        public DateTime Data { get; set; }
        public string Descrizione { get; set; }
        public string Targa { get; set; }
        public decimal Costo { get; set; }
    }
}