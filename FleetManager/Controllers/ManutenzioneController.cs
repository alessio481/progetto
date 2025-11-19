using FleetManager.Data;
using FleetManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FleetManager.Controllers
{
    [Authorize]
    public class ManutenzioneController : Controller
    {
        private readonly ILogger<ManutenzioneController> _logger;
        private readonly AppDbContext _context;

        public ManutenzioneController(ILogger<ManutenzioneController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Lista manutenzioni (accessibile anche senza login)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var manutenzioni = await _context.Manutenzioni
                .Include(m => m.Veicolo)
                .AsNoTracking()
                .OrderByDescending(m => m.DataCreazione)
                .ToListAsync();

            return View(manutenzioni);
        }

        // Dettagli di una manutenzione (accessibile anche senza login)
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var manutenzione = await _context.Manutenzioni
                .Include(m => m.Veicolo)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ManutenzioneID == id.Value);

            if (manutenzione == null) return NotFound();

            return View(manutenzione);
        }

        // GET: Create
        public async Task<IActionResult> Create()
        {
            ViewData["Veicoli"] = new SelectList(await _context.Veicoli.AsNoTracking().ToListAsync(), "VeicoloId", "Targa");
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VeicoloID,Tipo,DataProgrammata,DataCompletamento,Descrizione,Stato")] Manutenzione manutenzione)
        {
            if (ModelState.IsValid)
            {
                manutenzione.DataCreazione = DateTime.Now;
                _context.Add(manutenzione);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["Veicoli"] = new SelectList(await _context.Veicoli.AsNoTracking().ToListAsync(), "VeicoloId", "Targa", manutenzione.VeicoloID);
            return View(manutenzione);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var manutenzione = await _context.Manutenzioni.FindAsync(id.Value);
            if (manutenzione == null) return NotFound();

            ViewData["Veicoli"] = new SelectList(await _context.Veicoli.AsNoTracking().ToListAsync(), "VeicoloId", "Targa", manutenzione.VeicoloID);
            return View(manutenzione);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id)
        {
            var manutenzioneToUpdate = await _context.Manutenzioni.FindAsync(id);
            if (manutenzioneToUpdate == null) return NotFound();

            if (await TryUpdateModelAsync(manutenzioneToUpdate, "",
                m => m.VeicoloID, m => m.Tipo, m => m.DataProgrammata, m => m.DataCompletamento, m => m.Descrizione, m => m.Stato))
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Errore di concorrenza durante l'aggiornamento della manutenzione {Id}", id);
                    if (!ManutenzioneExists(id)) return NotFound();
                    throw;
                }
            }

            ViewData["Veicoli"] = new SelectList(await _context.Veicoli.AsNoTracking().ToListAsync(), "VeicoloId", "Targa", manutenzioneToUpdate.VeicoloID);
            return View(manutenzioneToUpdate);
        }

        // GET: Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var manutenzione = await _context.Manutenzioni
                .Include(m => m.Veicolo)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ManutenzioneID == id.Value);

            if (manutenzione == null) return NotFound();

            return View(manutenzione);
        }

        // POST: DeleteConfirmed
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manutenzione = await _context.Manutenzioni.FindAsync(id);
            if (manutenzione != null)
            {
                _context.Manutenzioni.Remove(manutenzione);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ManutenzioneExists(int id)
        {
            return _context.Manutenzioni.Any(e => e.ManutenzioneID == id);
        }
    }
}