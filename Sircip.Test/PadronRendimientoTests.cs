using System.Diagnostics;
using Sircip.Server.Padron.Models;
using Sircip.Server.Padron.Services;

namespace Sircip.Test;

/// <summary>
/// RNF-01: un padrón de un millón de registros se tiene que importar en menos
/// de un minuto.
/// </summary>
[Trait("Categoria", "Rendimiento")]
public class PadronRendimientoTests : IDisposable
{
    private const int Periodo = 202603;
    private const int CantidadRegistros = 1_000_000;
    private static readonly TimeSpan Limite = TimeSpan.FromMinutes(1);

    private readonly ITestOutputHelper _salida;
    private readonly string _directorio;

    public PadronRendimientoTests(ITestOutputHelper salida)
    {
        _salida = salida;
        _directorio = Directory.CreateTempSubdirectory("sircip-rendimiento-").FullName;
    }

    public void Dispose() => Directory.Delete(_directorio, recursive: true);

    [Fact]
    public void ParsearYEscribirUnMillonDeRegistros_TardaMenosDeUnMinuto()
    {
        var rutaTexto = Path.Combine(_directorio, "padron.txt");
        GenerarArchivoDePadron(rutaTexto, CantidadRegistros);
        var rutaBinaria = Path.Combine(_directorio, "padron.bin");

        var cronometro = Stopwatch.StartNew();

        var escritor = new EscritorPadronBinario(Periodo, CantidadRegistros);
        foreach (var linea in File.ReadLines(rutaTexto))
        {
            if (!ParserPadron.TryParsear(linea, out var registro, out var error))
            {
                Assert.Fail($"El generador armó una línea inválida: {error}");
            }

            escritor.Agregar(registro);
        }

        using (var archivo = File.Create(rutaBinaria))
        {
            Assert.Equal(CantidadRegistros, escritor.Escribir(archivo));
        }

        cronometro.Stop();

        _salida.WriteLine(
            $"Parsear y escribir {CantidadRegistros:N0} registros: {cronometro.Elapsed.TotalSeconds:N2} s " +
            $"(límite {Limite.TotalSeconds:N0} s). Archivo: {new FileInfo(rutaBinaria).Length / 1024d / 1024d:N1} MB.");

        using var lector = new LectorPadronBinario(rutaBinaria);
        Assert.Equal(CantidadRegistros, lector.CantidadRegistros);
        Assert.NotNull(lector.Buscar(CuitDeIndice(0)));
        Assert.NotNull(lector.Buscar(CuitDeIndice(CantidadRegistros / 2)));
        Assert.NotNull(lector.Buscar(CuitDeIndice(CantidadRegistros - 1)));

        Assert.True(
            cronometro.Elapsed < Limite,
            $"Parsear y escribir {CantidadRegistros:N0} registros tardó {cronometro.Elapsed.TotalSeconds:N1} s y el límite es {Limite.TotalSeconds:N0} s.");
    }

    /// <summary>Con el archivo mapeado, buscar por CUIT no depende del tamaño del padrón.</summary>
    [Fact]
    public void BuscarEnUnPadronDeUnMillonDeRegistros_EsInmediato()
    {
        var rutaTexto = Path.Combine(_directorio, "padron.txt");
        GenerarArchivoDePadron(rutaTexto, CantidadRegistros);
        var rutaBinaria = Path.Combine(_directorio, "padron.bin");

        var escritor = new EscritorPadronBinario(Periodo, CantidadRegistros);
        foreach (var linea in File.ReadLines(rutaTexto))
        {
            ParserPadron.TryParsear(linea, out var registro, out _);
            escritor.Agregar(registro!);
        }

        using (var archivo = File.Create(rutaBinaria))
        {
            escritor.Escribir(archivo);
        }

        using var lector = new LectorPadronBinario(rutaBinaria);

        // Una búsqueda inicial para que el archivo ya esté mapeado.
        lector.Buscar(CuitDeIndice(0));

        const int cantidadBusquedas = 10_000;
        var azar = new Random(1234);
        var cronometro = Stopwatch.StartNew();
        for (var i = 0; i < cantidadBusquedas; i++)
        {
            Assert.NotNull(lector.Buscar(CuitDeIndice(azar.Next(CantidadRegistros))));
        }

        cronometro.Stop();

        _salida.WriteLine(
            $"{cantidadBusquedas:N0} búsquedas: {cronometro.Elapsed.TotalMilliseconds:N0} ms " +
            $"({cronometro.Elapsed.TotalMilliseconds * 1000 / cantidadBusquedas:N1} µs por búsqueda).");

        Assert.True(
            cronometro.Elapsed < TimeSpan.FromSeconds(10),
            $"{cantidadBusquedas:N0} búsquedas tardaron {cronometro.Elapsed.TotalSeconds:N1} s.");
    }

    private static void GenerarArchivoDePadron(string ruta, int cantidad)
    {
        const string campo7 = "5225355222512555552512420";

        using var escritor = new StreamWriter(ruta);
        for (var i = 0; i < cantidad; i++)
        {
            var crc = 10 + i % 90;
            var letra = (char)('A' + i % 24);
            escritor.WriteLine($"{Periodo},{CuitDeIndice(i):00000000000},Contribuyente {i},904,{crc},{letra},{campo7}");
        }
    }

    /// <summary>CUIT distinto por índice, con su dígito verificador bien calculado.</summary>
    private static long CuitDeIndice(int indice)
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
