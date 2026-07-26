namespace Sircip.Server.Padron;

/// <summary>
/// Las 24 jurisdicciones del Convenio Multilateral, identificadas con los
/// códigos 901 a 924 (Anexo C). Están en el mismo orden en que las codifica el
/// Campo 7 del padrón, así que el código 900 + N es la posición N del campo.
/// </summary>
public static class Jurisdicciones
{
    public const int CodigoPrimera = 901;
    public const int CodigoUltima = 924;
    public const int Cantidad = CodigoUltima - CodigoPrimera + 1;

    public static bool EsCodigoValido(int codigo) => codigo is >= CodigoPrimera and <= CodigoUltima;

    /// <summary>Posición 0-based de la jurisdicción dentro del Campo 7 normalizado.</summary>
    public static int APosicion(int codigo)
    {
        if (!EsCodigoValido(codigo))
        {
            throw new ArgumentOutOfRangeException(
                nameof(codigo), codigo,
                $"El código de jurisdicción debe estar entre {CodigoPrimera} y {CodigoUltima}.");
        }

        return codigo - CodigoPrimera;
    }
}
