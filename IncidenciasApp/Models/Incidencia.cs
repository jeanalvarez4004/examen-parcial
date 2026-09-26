using System.ComponentModel.DataAnnotations;

namespace IncidenciasApp.Models;

public class Incidencia
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Estación")]
    public string Estacion { get; set; } = string.Empty;

    [Required, StringLength(500)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(1, 3)]
    public int Prioridad { get; set; } = 2;

    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

    [Display(Name = "Reportada")]
    public DateTime FechaReporte { get; set; } = DateTime.UtcNow;
}
