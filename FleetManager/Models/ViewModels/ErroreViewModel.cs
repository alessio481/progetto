namespace FleetManager.Models.ViewModels
{
    public class ErroreViewModel
    {
        public string? IdRichiesta { get; set; }

        public bool MostraIdRichiesta
        {
            get
            {
                return !string.IsNullOrEmpty(IdRichiesta);
            }
        }
    }
}
