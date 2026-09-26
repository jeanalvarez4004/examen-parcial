using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidenciasApp.Data;
using IncidenciasApp.Models;

namespace IncidenciasApp.Controllers;

// Pantalla base del examen. La linea del titulo es compartida:
// las ramas A, B y C la modifican y generan los conflictos de P4.
[Authorize]
[Route("Operaciones/[action]")]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;

    public OperacionesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET /Operaciones/Incidencias
    public async Task<IActionResult> Incidencias()
    {
        var list = await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(i => i.Prioridad)
            .ThenBy(i => i.FechaReporte)
            .ToListAsync();
        return View(list);
    }

    // POST /Operaciones/Cerrar/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var inc = await _context.Incidencias.FindAsync(id);
        if (inc is null) return NotFound();
        if (inc.Estado == EstadoIncidencia.Abierta)
        {
            inc.Estado = EstadoIncidencia.Cerrada;
            await _context.SaveChangesAsync();
            TempData["Exito"] = $"Incidencia #{id} cerrada.";
        }
        return RedirectToAction(nameof(Incidencias));
    }
}
