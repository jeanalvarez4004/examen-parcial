using Algolia.Search.Clients;
using Algolia.Search.Models.Search;
using Microsoft.Extensions.Options;
namespace IncidenciasApp.Services;

// P1/A: el servidor consulta Algolia y devuelve los Ids coincidentes.
// La clave de admin jamas sale del servidor (solo se usa la de busqueda).
public class AlgoliaSearchService
{
    private readonly AlgoliaOptions _opt;
    private readonly ILogger<AlgoliaSearchService> _log;

    public AlgoliaSearchService(IOptions<AlgoliaOptions> opt, ILogger<AlgoliaSearchService> log)
    {
        _opt = opt.Value;
        _log = log;
    }

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(_opt.AppId) && !string.IsNullOrWhiteSpace(_opt.ApiKey);

    public async Task<List<int>> BuscarIdsAsync(string texto, CancellationToken ct = default)
    {
        var client = new SearchClient(_opt.AppId, _opt.ApiKey);
        var res = await client.SearchSingleIndexAsync<Dictionary<string, object>>(
            _opt.IndexName,
            new SearchParams(new SearchParamsObject { Query = texto }));
        var ids = new List<int>();
        foreach (var hit in res.Hits)
        {
            if (hit.TryGetValue("objectID", out var oid) && int.TryParse(oid?.ToString(), out var id))
                ids.Add(id);
        }
        _log.LogInformation("Algolia devolvio {N} hits para '{Q}'.", ids.Count, texto);
        return ids;
    }
}
