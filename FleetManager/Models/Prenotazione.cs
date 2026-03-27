namespace FleetManager.Models
{
    public class Prenotazione
    {
        public int Id { get; set; }

        public int VeicoloId { get; set; }
        public Veicolo? Veicolo { get; set; }

        public int UtenteId { get; set; }
        public Utente? Utente { get; set; }

        // Quando l'utente prende in carico il veicolo.
        public DateTime OraPrenotazione { get; set; }

        // Quando smette di usarlo.
        public DateTime? OraRilascio { get; set; }
    }
}
