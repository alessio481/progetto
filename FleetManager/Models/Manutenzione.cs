using FleetManager.Models;

public class Manutenzione
{
    public int Id { get; set; }

    public int VeicoloId { get; set; }
    public Veicolo Veicolo { get; set; }

    public int UtenteId { get; set; }
    public Utente Utente { get; set; }

    public string Descrizione { get; set; }
    public DateTime DataInizio { get; set; }
    public DateTime? DataFine { get; set; }
}
