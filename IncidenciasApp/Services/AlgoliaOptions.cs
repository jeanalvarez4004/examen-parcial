namespace IncidenciasApp.Services;

// P1/A: opciones de Algolia por variables de entorno (solo servidor).
public class AlgoliaOptions
{
    public const string Section = "Algolia";
    public string AppId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty; // clave de BUSQUEDA, nunca la de admin
    public string IndexName { get; set; } = "incidencias";
}
