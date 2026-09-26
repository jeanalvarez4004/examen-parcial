using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace IncidenciasApp.Services;

// P3/C: publica eventos en PieHost desde el servidor via HTTP.
// Sin credenciales configuradas no hace nada (log) para no romper el cierre.
public class PieHostPublisher
{
    private readonly PieHostOptions _opt;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<PieHostPublisher> _log;

    public PieHostPublisher(IOptions<PieHostOptions> opt, IHttpClientFactory http, ILogger<PieHostPublisher> log)
    {
        _opt = opt.Value;
        _http = http;
        _log = log;
    }

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(_opt.ClusterId) && !string.IsNullOrWhiteSpace(_opt.Key);

    public async Task<bool> PublicarIncidenciaActualizadaAsync(int id, string estado, CancellationToken ct = default)
    {
        if (!Configurado)
        {
            _log.LogWarning("PieHost sin configurar; evento IncidenciaActualizada #{Id} no publicado.", id);
            return false;
        }
        try
        {
            var client = _http.CreateClient();
            var payload = new
            {
                key = _opt.Key,
                secret = _opt.Secret,
                roomId = _opt.Room,
                message = new { @event = "IncidenciaActualizada", data = new { Id = id, Estado = estado } }
            };
            var res = await client.PostAsync($"https://{_opt.ClusterId}.piesocket.com/api/publish",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
            _log.LogInformation("PieHost publish #{Id} -> {Status}.", id, (int)res.StatusCode);
            return res.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Fallo publicando en PieHost #{Id}.", id);
            return false;
        }
    }
}
