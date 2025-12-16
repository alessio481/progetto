using System.Collections.Generic;
using FleetManager.Models;

namespace FleetManager.Models.ViewModels
{
    public class VisualizzatoreDatiGenerali
    {
        public List<Veicolo> Veicoli { get; set; } = new();
        public List<Utente> Utenti { get; set; } = new();
        public List<Prenotazione> Prenotazioni { get; set; } = new();
        public List<Manutenzione> Manutenzioni { get; set; } = new();
        public List<Segnalazione> Segnalazioni { get; set; } = new();
        public List<DashboardSnapshot> DashboardSnapshots { get; set; } = new();
    }
}