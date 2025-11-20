using FleetManager.Models;

public class Prenotazione
{
    public int Id { get; set; }

    public int VeicoloId { get; set; }
    public Veicolo Veicolo { get; set; }

    public int UtenteId { get; set; }
    public Utente Utente { get; set; }

    public DateTime OraPrenotazione { get; set; }
    public DateTime? OraRilascio { get; set; } // quando finisce
}
