using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sircip.Shared.Serialization;

/// <summary>
/// Configuración de serialización JSON compartida entre la API y sus clientes.
///
/// Los enums viajan como texto ("Exitosa", "Administrador") y no como el número
/// de su posición, para que la respuesta se entienda sin tener el enum a mano y
/// para que agregar un valor en el medio no cambie el significado de los que ya
/// estaban.
///
/// Está en un solo lugar a propósito: si el servidor serializa distinto de como
/// deserializa el cliente, se rompe.
/// </summary>
public static class JsonSircip
{
    /// <summary>Aplica la configuración sobre las opciones que ya usa quien llama.</summary>
    public static void Configurar(JsonSerializerOptions opciones)
    {
        opciones.Converters.Add(new JsonStringEnumConverter());
    }

    /// <summary>Opciones listas para usar con <c>ReadFromJsonAsync</c> y afines.</summary>
    public static JsonSerializerOptions Opciones { get; } = Crear();

    private static JsonSerializerOptions Crear()
    {
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Configurar(opciones);
        return opciones;
    }
}
