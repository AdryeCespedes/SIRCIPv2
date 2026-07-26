namespace Sircip.Server.Padron;

/// <summary>
/// Set de alícuotas del Campo 6 del padrón (Anexo A, nota 2). Cada letra
/// representa un porcentaje: la letra B es 0,01 %, no 0,01.
/// </summary>
public static class SetAlicuotas
{
    private static readonly Dictionary<char, decimal> PorcentajePorLetra = new()
    {
        ['A'] = 0.00m,
        ['B'] = 0.01m,
        ['C'] = 0.05m,
        ['D'] = 0.10m,
        ['E'] = 0.20m,
        ['F'] = 0.30m,
        ['G'] = 0.40m,
        ['H'] = 0.50m,
        ['I'] = 0.60m,
        ['J'] = 0.70m,
        ['K'] = 0.80m,
        ['L'] = 1.00m,
        ['M'] = 1.20m,
        ['N'] = 1.40m,
        ['O'] = 1.50m,
        ['P'] = 1.60m,
        ['Q'] = 1.80m,
        ['R'] = 2.00m,
        ['S'] = 2.50m,
        ['T'] = 3.00m,
        ['U'] = 3.50m,
        ['V'] = 4.00m,
        ['W'] = 4.50m,
        ['X'] = 5.00m
    };

    public static bool EsLetraValida(char letra) => PorcentajePorLetra.ContainsKey(letra);

    /// <summary>Devuelve el porcentaje de la letra (por ejemplo 2.50m para la S).</summary>
    public static decimal ObtenerPorcentaje(char letra) =>
        PorcentajePorLetra.TryGetValue(letra, out var porcentaje)
            ? porcentaje
            : throw new ArgumentOutOfRangeException(
                nameof(letra), letra, "La letra no pertenece al set de alícuotas del Anexo A.");
}
