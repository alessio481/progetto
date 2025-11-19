using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Controllers
{
    public class AssignmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AssignmentsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var assignments = await _context.Assignments
                .Include(a => a.Vehicle)
                .Include(a => a.Driver)
                .OrderByDescending(a => a.DataInizio)
                .ToListAsync();
            return View(assignments);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateSelections();
            return View(new Assignment());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Assignment assignment)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSelections();
                return View(assignment);
            }

            var vehicle = await _context.Vehicles.FindAsync(assignment.VehicleId);
            if (vehicle != null)
            {
                vehicle.Stato = VehicleStatus.Assegnato;
            }

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment is null)
            {
                return NotFound();
            }

            await PopulateSelections();
            return View(assignment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Assignment assignment)
        {
            if (id != assignment.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelections();
                return View(assignment);
            }

            _context.Update(assignment);

            if (assignment.DataFineEffettiva.HasValue)
            {
                var vehicle = await _context.Vehicles.FindAsync(assignment.VehicleId);
                if (vehicle != null)
                {
                    vehicle.Stato = VehicleStatus.Disponibile;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelections()
        {
            ViewBag.Vehicles = new SelectList(await _context.Vehicles.ToListAsync(), "Id", "Targa");
            ViewBag.Drivers = new SelectList(await _context.Drivers.ToListAsync(), "Id", "Nome");
        }
    }
}
