using System.Net.Http.Json;
using Sircip.Shared.Contracts;
using Sircip.Shared.Serialization;

namespace Sircip.Client.Services;

public class AuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("SircipApi");
    }

    public async Task<LoginResponse?> LoginAsync(string nombreUsuario, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            NombreUsuario = nombreUsuario,
            Password = password
        });

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<LoginResponse>(JsonSircip.Opciones);
    }
}
