namespace Sircip.Shared.Validations;

/// <summary>
/// Valida el dígito verificador del CUIT: módulo 11 sobre los primeros 10
/// dígitos con los ponderadores 5-4-3-2-7-6-5-4-3-2.
///
/// Vive en Shared porque lo usan tanto la validación del padrón (Campo 2 del
/// Anexo A) como el CUIT que se ingresa para calcular una percepción.
/// </summary>
public static class ValidadorCuit
{
    public const int LargoCuit = 11;

    private static readonly int[] Ponderadores = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

    public static bool EsValido(ReadOnlySpan<char> cuit)
    {
        if (cuit.Length != LargoCuit)
        {
            return false;
        }

        var suma = 0;
        for (var i = 0; i < Ponderadores.Length; i++)
        {
            if (!char.IsAsciiDigit(cuit[i]))
            {
                return false;
            }

            suma += (cuit[i] - '0') * Ponderadores[i];
        }

        if (!char.IsAsciiDigit(cuit[^1]))
        {
            return false;
        }

        return cuit[^1] - '0' == CalcularDigitoVerificador(suma);
    }

    private static int CalcularDigitoVerificador(int suma) => (11 - suma % 11) switch
    {
        11 => 0,
        // AFIP no asigna el dígito verificador 10: esos CUIT se reasignan con
        // dígito 9 (y prefijo 23).
        10 => 9,
        var digito => digito
    };
}
