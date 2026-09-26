namespace IncidenciasApp.Services;

// P3/C: opciones de PieHost por variables de entorno (solo servidor guarda el Secret).
public class PieHostOptions
{
    public const string Section = "PieHost";
    public string ClusterId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; // clave publica, puede ir al navegador
    public string Secret { get; set; } = string.Empty; // NUNCA al navegador
    public string Room { get; set; } = "incidencias";
}
