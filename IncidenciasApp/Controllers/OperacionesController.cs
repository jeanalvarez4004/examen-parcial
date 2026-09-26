using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IncidenciasApp.Data;
using IncidenciasApp.Models;
using IncidenciasApp.Services;

namespace IncidenciasApp.Controllers;

// Pantalla base del examen. La linea del titulo es compartida:
// las ramas A, B y C la modifican y generan los conflictos de P4.
[Authorize]
[Route("Operaciones/[action]")]
public class OperacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AlgoliaSearchService _algolia;
    private readonly ILogger<OperacionesController> _log;

    public OperacionesController(
        ApplicationDbContext context,
        AlgoliaSearchService algolia,
        ILogger<OperacionesController> log)
    {
        _context = context;
        _algolia = algolia;
        _log = log;
    }

    // GET /Operaciones/Incidencias?q=
    public async Task<IActionResult> Incidencias(string? q)
    {
        ViewBag.Q = q;
        var baseQ = _context.Incidencias.Where(i => i.Estado == EstadoIncidencia.Abierta);

        if (!string.IsNullOrWhiteSpace(q))
        {
            // P1/A: con texto se consulta Algolia (servidor) y se filtra a abiertas en base.
            if (_algolia.Configurado)
            {
                var ids = await _algolia.BuscarIdsAsync(q.Trim());
                var list = await baseQ.Where(i => ids.Contains(i.Id))
                    .OrderByDescending(i => i.Prioridad).ThenBy(i => i.FechaReporte)
                    .ToListAsync();
                ViewBag.Fuente = "Algolia";
                return View(list);
            }
            // Sin credenciales: respaldo local con el mismo contrato.
            _log.LogWarning("Algolia sin configurar; busqueda local de respaldo.");
            var t = q.Trim();
            var local = await baseQ.Where(i => i.Estacion.Contains(t) || i.Descripcion.Contains(t))
                .OrderByDescending(i => i.Prioridad).ThenBy(i => i.FechaReporte)
                .ToListAsync();
            ViewBag.Fuente = "Local (Algolia sin configurar)";
            return View(local);
        }

        // Sin texto: lista habitual.
        var todas = await baseQ.OrderByDescending(i => i.Prioridad).ThenBy(i => i.FechaReporte).ToListAsync();
        return View(todas);
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
