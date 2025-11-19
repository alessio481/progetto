namespace FleetManager.Models
{
    public class DashboardData
    {
        public int VeicoliTotali { get; set; }
        public int VeicoliDisponibili { get; set; }
        public int VeicoliAssegnati { get; set; }
        public int VeicoliInManutenzione { get; set; }
        public int AssegnazioniAperte { get; set; }
        public List<Assignment> UltimeAssegnazioni { get; set; } = new();
    }
}
