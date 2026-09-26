using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
    private readonly PieHostPublisher _pie;
    private readonly PieHostOptions _pieOpt;

    public OperacionesController(
        ApplicationDbContext context,
        PieHostPublisher pie,
        IOptions<PieHostOptions> pieOpt)
    {
        _context = context;
        _pie = pie;
        _pieOpt = pieOpt.Value;
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
            // P3/C: primero se guarda el estado...
            inc.Estado = EstadoIncidencia.Cerrada;
            await _context.SaveChangesAsync();
            // ...despues se publica el evento.
            await _pie.PublicarIncidenciaActualizadaAsync(inc.Id, inc.Estado.ToString());
            TempData["Exito"] = $"Incidencia #{id} cerrada.";
        }
        return RedirectToAction(nameof(Incidencias));
    }

    // Config publica para el navegador (sin Secret) + estado vigente para resync.
    [HttpGet]
    public IActionResult RealtimeConfig()
    {
        if (!_pie.Configurado) return Json(new { configurado = false });
        return Json(new { configurado = true, clusterId = _pieOpt.ClusterId, apiKey = _pieOpt.Key, room = _pieOpt.Room });
    }

    [HttpGet]
    public async Task<IActionResult> AbiertasJson()
    {
        var ids = await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .Select(i => i.Id)
            .ToListAsync();
        return Json(ids);
    }
}
