using Microsoft.AspNetCore.Mvc;

namespace FleetManager.Controllers
{
    public class VeicoliController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}
