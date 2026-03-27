namespace FleetManager.Models
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
