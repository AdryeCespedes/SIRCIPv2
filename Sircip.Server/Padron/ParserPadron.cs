using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Sircip.Shared.Validaciones;

namespace Sircip.Server.Padron;

/// <summary>
/// Valida y parsea las líneas del archivo .txt del padrón contra el diseño de
/// registro del Anexo A (RF-11).
///
/// Solo controla el formato de los campos. Que el período declarado en la
/// importación coincida con el del Campo 1 es una regla de la importación
/// (RF-03) y se verifica allá, no acá.
///
/// Trabaja sobre <see cref="ReadOnlySpan{T}"/> y sin asignaciones intermedias
/// porque tiene que procesar un millón de líneas en menos de un minuto (RNF-01).
/// </summary>
public static class ParserPadron
{
    public const int CantidadCampos = 7;
    public const int LargoPeriodo = 6;
    public const int LargoCuit = 11;
    public const int LargoMaximoRazonSocial = 70;
    public const int LargoJurisdiccionSede = 3;
    public const int LargoCrc = 2;
    public const int LargoCampo7 = 25;

    /// <summary>Jurisdicciones del Convenio Multilateral codificadas en el Campo 7.</summary>
    public const int CantidadJurisdicciones = Jurisdicciones.Cantidad;

    /// <summary>
    /// Intenta parsear una línea del padrón. Si devuelve <c>false</c>,
    /// <paramref name="error"/> describe el campo que no cumple el formato, para
    /// poder registrarlo según RF-08.
    /// </summary>
    public static bool TryParsear(
        ReadOnlySpan<char> linea,
        [NotNullWhen(true)] out RegistroPadron? registro,
        [NotNullWhen(false)] out string? error)
    {
        registro = null;

        // Un rango de más que los campos esperados: así una línea con campos de
        // sobra devuelve una cantidad distinta en vez de pasar desapercibida.
        Span<Range> campos = stackalloc Range[CantidadCampos + 1];
        var cantidadCampos = linea.Split(campos, ',');
        if (cantidadCampos != CantidadCampos)
        {
            error = $"Se esperaban {CantidadCampos} campos separados por coma y se encontraron {cantidadCampos}.";
            return false;
        }

        if (!TryParsearPeriodo(linea[campos[0]], out var periodo, out error) ||
            !TryParsearCuit(linea[campos[1]], out var cuit, out error) ||
            !TryParsearRazonSocial(linea[campos[2]], out var razonSocial, out error) ||
            !TryParsearJurisdiccionSede(linea[campos[3]], out var jurisdiccionSede, out error) ||
            !TryParsearCrc(linea[campos[4]], out var crc, out error) ||
            !TryParsearLetraAlicuota(linea[campos[5]], out var letraAlicuota, out error) ||
            !TryParsearCampo7(linea[campos[6]], out var campo7, out error))
        {
            return false;
        }

        registro = new RegistroPadron
        {
            Periodo = periodo,
            Cuit = cuit,
            RazonSocial = razonSocial,
            JurisdiccionSede = jurisdiccionSede,
            Crc = crc,
            LetraAlicuota = letraAlicuota,
            Campo7 = campo7
        };
        return true;
    }

    private static bool TryParsearPeriodo(
        ReadOnlySpan<char> campo,
        out int periodo,
        [NotNullWhen(false)] out string? error)
    {
        periodo = 0;

        if (campo.Length != LargoPeriodo || !SonTodosDigitos(campo))
        {
            error = $"Campo 1 (período): se esperaban {LargoPeriodo} dígitos con formato aaaamm y se encontró '{campo}'.";
            return false;
        }

        periodo = int.Parse(campo, CultureInfo.InvariantCulture);

        var mes = periodo % 100;
        if (mes is < 1 or > 12)
        {
            error = $"Campo 1 (período): el mes '{mes:00}' no está entre 01 y 12.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryParsearCuit(
        ReadOnlySpan<char> campo,
        out long cuit,
        [NotNullWhen(false)] out string? error)
    {
        cuit = 0;

        if (campo.Length != LargoCuit || !SonTodosDigitos(campo))
        {
            error = $"Campo 2 (CUIT): se esperaban {LargoCuit} dígitos y se encontró '{campo}'.";
            return false;
        }

        if (!ValidadorCuit.EsValido(campo))
        {
            error = $"Campo 2 (CUIT): el dígito verificador de '{campo}' es incorrecto.";
            return false;
        }

        cuit = long.Parse(campo, CultureInfo.InvariantCulture);
        error = null;
        return true;
    }

    private static bool TryParsearRazonSocial(
        ReadOnlySpan<char> campo,
        [NotNullWhen(true)] out string? razonSocial,
        [NotNullWhen(false)] out string? error)
    {
        razonSocial = null;

        if (campo.Length > LargoMaximoRazonSocial)
        {
            error = $"Campo 3 (razón social): tiene {campo.Length} caracteres y el máximo es {LargoMaximoRazonSocial}.";
            return false;
        }

        var recortada = campo.Trim();
        if (recortada.IsEmpty)
        {
            error = "Campo 3 (razón social): no puede estar vacío.";
            return false;
        }

        razonSocial = recortada.ToString();
        error = null;
        return true;
    }

    private static bool TryParsearJurisdiccionSede(
        ReadOnlySpan<char> campo,
        out short jurisdiccionSede,
        [NotNullWhen(false)] out string? error)
    {
        jurisdiccionSede = 0;

        if (campo.Length != LargoJurisdiccionSede || !SonTodosDigitos(campo))
        {
            error = $"Campo 4 (jurisdicción sede): se esperaban {LargoJurisdiccionSede} dígitos y se encontró '{campo}'.";
            return false;
        }

        jurisdiccionSede = short.Parse(campo, CultureInfo.InvariantCulture);
        error = null;
        return true;
    }

    private static bool TryParsearCrc(
        ReadOnlySpan<char> campo,
        out byte crc,
        [NotNullWhen(false)] out string? error)
    {
        crc = 0;

        if (campo.Length != LargoCrc || !SonTodosDigitos(campo))
        {
            error = $"Campo 5 (CRC): se esperaban {LargoCrc} dígitos y se encontró '{campo}'.";
            return false;
        }

        // El Anexo A acota el CRC al rango 10-99, así que un cero a la izquierda
        // ('05') tiene el largo correcto pero no es un CRC válido.
        var valor = byte.Parse(campo, CultureInfo.InvariantCulture);
        if (valor < 10)
        {
            error = $"Campo 5 (CRC): el valor '{campo}' no está entre 10 y 99.";
            return false;
        }

        crc = valor;
        error = null;
        return true;
    }

    private static bool TryParsearLetraAlicuota(
        ReadOnlySpan<char> campo,
        out char letraAlicuota,
        [NotNullWhen(false)] out string? error)
    {
        letraAlicuota = default;

        if (campo.Length != 1 || !SetAlicuotas.EsLetraValida(campo[0]))
        {
            error = $"Campo 6 (letra de alícuota): '{campo}' no pertenece al set de alícuotas del Anexo A.";
            return false;
        }

        letraAlicuota = campo[0];
        error = null;
        return true;
    }

    private static bool TryParsearCampo7(
        ReadOnlySpan<char> campo,
        [NotNullWhen(true)] out string? campo7,
        [NotNullWhen(false)] out string? error)
    {
        campo7 = null;

        if (campo.Length != LargoCampo7 || !SonTodosDigitos(campo))
        {
            error = $"Campo 7: se esperaban {LargoCampo7} dígitos y se encontró '{campo}'.";
            return false;
        }

        if (campo[^1] != '0')
        {
            error = $"Campo 7: la última posición siempre debe ser 0 y es '{campo[^1]}'.";
            return false;
        }

        // Se lee de derecha a izquierda descartando la última posición: la
        // anteúltima es la jurisdicción 1 y la primera es la jurisdicción 24.
        for (var jurisdiccion = 1; jurisdiccion <= CantidadJurisdicciones; jurisdiccion++)
        {
            var estado = campo[LargoCampo7 - 1 - jurisdiccion];
            if (estado is < '1' or > '5')
            {
                error = $"Campo 7: la jurisdicción {jurisdiccion} tiene el estado '{estado}' y solo se admiten los valores 1 a 5.";
                return false;
            }
        }

        campo7 = campo.ToString();
        error = null;
        return true;
    }

    private static bool SonTodosDigitos(ReadOnlySpan<char> campo)
    {
        foreach (var caracter in campo)
        {
            if (!char.IsAsciiDigit(caracter))
            {
                return false;
            }
        }

        return true;
    }
}
