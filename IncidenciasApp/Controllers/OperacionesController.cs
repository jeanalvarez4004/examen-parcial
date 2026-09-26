using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
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
    private readonly IDistributedCache _cache;
    private readonly ILogger<OperacionesController> _log;

    public OperacionesController(
        ApplicationDbContext context,
        IDistributedCache cache,
        ILogger<OperacionesController> log)
    {
        _context = context;
        _cache = cache;
        _log = log;
    }

    public const string CacheKeyAbiertas = "incidencias:abiertas";

    // GET /Operaciones/Incidencias — listado cacheado 60s en Redis.
    public async Task<IActionResult> Incidencias()
    {
        var hit = await _cache.GetStringAsync(CacheKeyAbiertas);
        if (hit is not null)
        {
            _log.LogInformation("Listado de abiertas desde REDIS.");
            ViewBag.Fuente = "Redis";
            return View(JsonSerializer.Deserialize<List<Incidencia>>(hit)!);
        }
        _log.LogInformation("Listado de abiertas desde BASE DE DATOS.");
        var list = await _context.Incidencias
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderByDescending(i => i.Prioridad)
            .ThenBy(i => i.FechaReporte)
            .ToListAsync();
        await _cache.SetStringAsync(CacheKeyAbiertas, JsonSerializer.Serialize(list),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) });
        ViewBag.Fuente = "Base de datos";
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
            // P2/B: invalida la clave antes de volver a consultar.
            await _cache.RemoveAsync(CacheKeyAbiertas);
            _log.LogInformation("Cache {Key} invalidada al cerrar #{Id}.", CacheKeyAbiertas, id);
            TempData["Exito"] = $"Incidencia #{id} cerrada.";
        }
        return RedirectToAction(nameof(Incidencias));
    }
}
