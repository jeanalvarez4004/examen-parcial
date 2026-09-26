using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
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
    private readonly IDistributedCache _cache;
    private readonly AlgoliaSearchService _algolia;
    private readonly ILogger<OperacionesController> _log;

    public OperacionesController(
        ApplicationDbContext context,
        IDistributedCache cache,
        AlgoliaSearchService algolia,
        ILogger<OperacionesController> log)
    {
        _context = context;
        _cache = cache;
        _algolia = algolia;
        _log = log;
    }

    public const string CacheKeyAbiertas = "incidencias:abiertas";

    // GET /Operaciones/Incidencias?q=
    // Sin texto: listado general cacheado 60s (P2/B).
    // Con texto: Algolia directo, sin usar esta cache (P1/A).
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

        // P2/B: sin filtros => cache 60s.
        var hit = await _cache.GetStringAsync(CacheKeyAbiertas);
        if (hit is not null)
        {
            _log.LogInformation("Listado de abiertas desde REDIS.");
            ViewBag.Fuente = "Redis";
            return View(JsonSerializer.Deserialize<List<Incidencia>>(hit)!);
        }
        _log.LogInformation("Listado de abiertas desde BASE DE DATOS.");
        var todas = await baseQ.OrderByDescending(i => i.Prioridad).ThenBy(i => i.FechaReporte).ToListAsync();
        await _cache.SetStringAsync(CacheKeyAbiertas, JsonSerializer.Serialize(todas),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60) });
        ViewBag.Fuente = "Base de datos";
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
            // P2/B: invalida la clave antes de volver a consultar.
            await _cache.RemoveAsync(CacheKeyAbiertas);
            _log.LogInformation("Cache {Key} invalidada al cerrar #{Id}.", CacheKeyAbiertas, id);
            TempData["Exito"] = $"Incidencia #{id} cerrada.";
        }
        return RedirectToAction(nameof(Incidencias));
    }
}
