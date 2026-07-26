namespace Sircip.Test;

/// <summary>Arma líneas de padrón válidas según el Anexo A para usar en los tests.</summary>
internal static class PadronDePrueba
{
    /// <summary>Campo 7 del registro de ejemplo del Anexo A.</summary>
    public const string Campo7 = "5225355222512555552512420";

    public static string Linea(int periodo, long cuit, int crc = 34, char letra = 'B', string? campo7 = null) =>
        $"{periodo},{cuit:00000000000},Contribuyente {cuit},904,{crc:00},{letra},{campo7 ?? Campo7}";

    /// <summary>CUIT distinto por índice, con su dígito verificador bien calculado.</summary>
    public static long Cuit(int indice)
    {
        var primerosDiez = 2_000_000_000L + indice;
        return primerosDiez * 10 + DigitoVerificador(primerosDiez);
    }

    private static int DigitoVerificador(long primerosDiez)
    {
        int[] ponderadores = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        var digitos = primerosDiez.ToString("0000000000");

        var suma = 0;
        for (var i = 0; i < ponderadores.Length; i++)
        {
            suma += (digitos[i] - '0') * ponderadores[i];
        }

        return (11 - suma % 11) switch { 11 => 0, 10 => 9, var digito => digito };
    }
}
