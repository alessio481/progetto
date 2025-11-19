using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FleetManagementSystem.Controllers
{
    [Authorize(Roles = "User")]
    public class UserController : Controller
    {
        // Dashboard utente
        public IActionResult Dashboard()
        {
            // Qui puoi aggiungere la logica per ottenere i dati dell'utente
            var userDashboardModel = new UserDashboardViewModel
            {
                UserName = User.Identity.Name,
                CurrentVehicle = new CurrentVehicleInfo
                {
                    LicensePlate = "AA-12345",
                    Model = "Toyota RAV4 - SUV",
                    AssignmentDate = new DateTime(2023, 10, 1),
                    ExpectedReturnDate = new DateTime(2023, 10, 31),
                    FuelLevel = 65,
                    OverallStatus = "Ottimo"
                },
                Requests = new List<UserRequest>
                {
                    new UserRequest
                    {
                        Date = new DateTime(2023, 10, 1),
                        Type = "Richiesta veicolo",
                        Description = "SUV per viaggio di lavoro a Milano",
                        Status = "Approvata"
                    },
                    new UserRequest
                    {
                        Date = new DateTime(2023, 10, 5),
                        Type = "Segnalazione problema",
                        Description = "Problema con l'aria condizionata",
                        Status = "In Lavorazione"
                    },
                    new UserRequest
                    {
                        Date = new DateTime(2023, 10, 10),
                        Type = "Richiesta veicolo",
                        Description = "Furgone per consegna materiale",
                        Status = "In Attesa"
                    }
                }
            };

            return View(userDashboardModel);
        }

        // Altre funzionalità utente...
        public IActionResult MyVehicles()
        {
            return View();
        }

        public IActionResult RequestVehicle()
        {
            return View();
        }

        [HttpPost]
        public IActionResult RequestVehicle(VehicleRequestModel model)
        {
            if (ModelState.IsValid)
            {
                // Gestisci la logica della richiesta veicolo
                // Salva nel database, invia notifiche, ecc.

                TempData["SuccessMessage"] = "Richiesta veicolo inviata con successo!";
                return RedirectToAction("Dashboard");
            }

            return View(model);
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult History()
        {
            return View();
        }

        public IActionResult Costs()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }
    }

    // Modello di vista dashboard utente
    public class UserDashboardViewModel
    {
        public string UserName { get; set; }
        public CurrentVehicleInfo CurrentVehicle { get; set; }
        public List<UserRequest> Requests { get; set; }
    }

    public class CurrentVehicleInfo
    {
        public string LicensePlate { get; set; }
        public string Model { get; set; }
        public DateTime AssignmentDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public int FuelLevel { get; set; }
        public string OverallStatus { get; set; }
    }

    public class UserRequest
    {
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
    }

    public class VehicleRequestModel
    {
        [Required(ErrorMessage = "Il campo Scopo è obbligatorio")]
        public string Purpose { get; set; }

        [Required(ErrorMessage = "Il campo Data e Ora è obbligatorio")]
        public DateTime RequestedDateTime { get; set; }

        public string Notes { get; set; }
    }
}