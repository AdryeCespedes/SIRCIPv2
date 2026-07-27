using System.Net;
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

        var respuesta = await EnviarAsync(pedido);
        respuesta.EnsureSuccessStatusCode();

        return await respuesta.Content.ReadFromJsonAsync<List<ImportacionResponse>>(JsonSircip.Opciones) ?? [];
    }

    /// <summary>Importa el padrón de un período (RF-03).</summary>
    public async Task<ResultadoImportacion> ImportarAsync(int anio, int mes, string rutaArchivo)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Post, "api/padron/importaciones")
        {
            Content = JsonContent.Create(
                new ImportarPadronRequest { Anio = anio, Mes = mes, RutaArchivo = rutaArchivo },
                options: JsonSircip.Opciones)
        };

        var respuesta = await EnviarAsync(pedido);

        if (respuesta.IsSuccessStatusCode)
        {
            var importacion = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>(JsonSircip.Opciones);
            return ResultadoImportacion.Ok(importacion!);
        }

        // El 422 trae la constancia del intento fallido, con el motivo adentro;
        // el 400 y el 409 devuelven el mensaje como texto plano.
        if (respuesta.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            var fallida = await respuesta.Content.ReadFromJsonAsync<ImportacionResponse>(JsonSircip.Opciones);
            return ResultadoImportacion.Fallo(
                fallida?.Error ?? "No se pudo importar el archivo del padrón.", fallida);
        }

        var mensaje = await respuesta.Content.ReadAsStringAsync();
        return ResultadoImportacion.Fallo(
            string.IsNullOrWhiteSpace(mensaje)
                ? $"La importación falló con el código {(int)respuesta.StatusCode}."
                : mensaje);
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpRequestMessage pedido)
    {
        var token = await ObtenerTokenAsync();
        if (token is not null)
        {
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _httpClient.SendAsync(pedido);
    }

    private async Task<string?> ObtenerTokenAsync()
    {
        var estado = await _proveedorDeAutenticacion.GetAuthenticationStateAsync();
        return estado.User.FindFirst(ClaimDelToken)?.Value;
    }
}
