using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IncidenciasApp.Models;

namespace IncidenciasApp.Data;

// Datos de prueba: supervisor + incidencias de estaciones de bicicletas.
public static class SeedData
{
    public const string SupervisorEmail = "supervisor@empresa.pe";
    public const string SupervisorPassword = "Supervisor123*";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var context = sp.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(SupervisorEmail);
        if (user is null)
        {
            user = new IdentityUser { UserName = SupervisorEmail, Email = SupervisorEmail, EmailConfirmed = true };
            var r = await userManager.CreateAsync(user, SupervisorPassword);
            if (!r.Succeeded)
                throw new InvalidOperationException("Seed usuario: " + string.Join("; ", r.Errors.Select(e => e.Description)));
        }

        if (!await context.Incidencias.AnyAsync())
        {
            context.Incidencias.AddRange(
                new Incidencia { Estacion = "Estación Central", Descripcion = "Freno delantero averiado en bicicleta B-014", Prioridad = 3, Estado = EstadoIncidencia.Abierta, FechaReporte = DateTime.UtcNow.AddHours(-5) },
                new Incidencia { Estacion = "Parque Norte", Descripcion = "Anclaje 7 no libera bicicletas", Prioridad = 2, Estado = EstadoIncidencia.Abierta, FechaReporte = DateTime.UtcNow.AddHours(-3) },
                new Incidencia { Estacion = "Malecón Sur", Descripcion = "Llanta ponchada en bicicleta B-032", Prioridad = 1, Estado = EstadoIncidencia.Abierta, FechaReporte = DateTime.UtcNow.AddHours(-1) },
                new Incidencia { Estacion = "Estación Central", Descripcion = "Pantalla del tótem sin imagen", Prioridad = 2, Estado = EstadoIncidencia.Cerrada, FechaReporte = DateTime.UtcNow.AddDays(-2) });
            await context.SaveChangesAsync();
        }
    }
}
