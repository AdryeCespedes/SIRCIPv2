using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Sircip.Shared.Contracts;
using Sircip.Shared.Serialization;

namespace Sircip.Client.Services;

/// <summary>
/// Consulta los endpoints del padrón.
///
/// La sesión del cliente es una cookie, pero la API espera un JWT: el token se
/// guardó como claim al iniciar sesión y se adjunta acá en cada pedido.
/// </summary>
public class PadronApiClient
{
    private const string ClaimDelToken = "jwt";

    private readonly HttpClient _httpClient;
    private readonly AuthenticationStateProvider _proveedorDeAutenticacion;

    public PadronApiClient(
        IHttpClientFactory httpClientFactory,
        AuthenticationStateProvider proveedorDeAutenticacion)
    {
        _httpClient = httpClientFactory.CreateClient("SircipApi");
        _proveedorDeAutenticacion = proveedorDeAutenticacion;
    }

    /// <summary>Historial de importaciones, exitosas y con error (RF-10).</summary>
    public async Task<IReadOnlyList<ImportacionResponse>> ObtenerImportacionesAsync()
    {
        var pedido = new HttpRequestMessage(HttpMethod.Get, "api/padron/importaciones");

        var token = await ObtenerTokenAsync();
        if (token is not null)
        {
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var respuesta = await _httpClient.SendAsync(pedido);
        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<List<ImportacionResponse>>(JsonSircip.Opciones) ?? [];
    }

    private async Task<string?> ObtenerTokenAsync()
    {
        var estado = await _proveedorDeAutenticacion.GetAuthenticationStateAsync();
        return estado.User.FindFirst(ClaimDelToken)?.Value;
    }
}
