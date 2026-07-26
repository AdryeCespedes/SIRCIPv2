using System.Buffers.Binary;
using Sircip.Server.Padron.Exceptions;
using Sircip.Server.Padron.Models;
using Sircip.Server.Padron.Services;

namespace Sircip.Test;

/// <summary>
/// Ida y vuelta por el archivo binario del padrón: se escribe con
/// <see cref="EscritorPadronBinario"/> y se busca con
/// <see cref="LectorPadronBinario"/>.
/// </summary>
public class PadronBinarioTests : IDisposable
{
    private const int Periodo = 202603;

    private readonly string _directorio;

    public PadronBinarioTests()
    {
        _directorio = Directory.CreateTempSubdirectory("sircip-padron-").FullName;
    }

    public void Dispose() => Directory.Delete(_directorio, recursive: true);

    private string RutaNueva() => Path.Combine(_directorio, $"{Guid.NewGuid():N}.bin");

    /// <summary>Campo 7 con un estado distinto por jurisdicción, para detectar si se dan vuelta mal.</summary>
    private static string Campo7Con(int codigoJurisdiccion, char estado)
    {
        var campo7 = new char[ParserPadron.LargoCampo7];
        Array.Fill(campo7, '1');
        campo7[^1] = '0';
        campo7[ParserPadron.LargoCampo7 - 2 - (codigoJurisdiccion - Jurisdicciones.CodigoPrimera)] = estado;
        return new string(campo7);
    }

    private static RegistroPadron Registro(long cuit, byte crc = 34, char letra = 'B', string? campo7 = null) =>
        new()
        {
            Periodo = Periodo,
            Cuit = cuit,
            RazonSocial = $"Contribuyente {cuit}",
            JurisdiccionSede = 904,
            Crc = crc,
            LetraAlicuota = letra,
            Campo7 = campo7 ?? new string('1', ParserPadron.LargoCampo7 - 1) + '0'
        };

    private string EscribirPadron(params RegistroPadron[] registros)
    {
        var ruta = RutaNueva();
        var escritor = new EscritorPadronBinario(Periodo);
        foreach (var registro in registros)
        {
            escritor.Agregar(registro);
        }

        using var archivo = File.Create(ruta);
        escritor.Escribir(archivo);
        return ruta;
    }

    [Fact]
    public void Escribir_YLuegoBuscar_DevuelveLosMismosDatos()
    {
        var ruta = EscribirPadron(Registro(30100100106, crc: 47, letra: 'S'));

        using var lector = new LectorPadronBinario(ruta);
        var contribuyente = lector.Buscar(30100100106);

        Assert.NotNull(contribuyente);
        Assert.Equal(30100100106L, contribuyente.Cuit);
        Assert.Equal((byte)47, contribuyente.Crc);
        Assert.Equal('S', contribuyente.LetraAlicuota);
    }

    [Fact]
    public void Escribir_GuardaElPeriodoYLaCantidadEnElEncabezado()
    {
        var ruta = EscribirPadron(Registro(30100100106), Registro(20123456786), Registro(27123456780));

        using var lector = new LectorPadronBinario(ruta);

        Assert.Equal(Periodo, lector.Periodo);
        Assert.Equal(3, lector.CantidadRegistros);
    }

    [Fact]
    public void Buscar_ConUnCuitQueNoEstaEnElPadron_DevuelveNull()
    {
        var ruta = EscribirPadron(Registro(30100100106), Registro(20123456786));

        using var lector = new LectorPadronBinario(ruta);

        Assert.Null(lector.Buscar(33693450239));
        Assert.False(lector.Contiene(33693450239));
    }

    /// <summary>
    /// La búsqueda binaria tiene que encontrar cualquier registro, no solo los
    /// del medio: se escriben desordenados y se buscan todos.
    /// </summary>
    [Fact]
    public void Buscar_EncuentraTodosLosRegistrosAunqueSeHayanAgregadoDesordenados()
    {
        var cuits = new long[] { 30100100106, 20123456786, 33693450239, 27123456780, 23200000039 };
        var ruta = EscribirPadron(cuits.Select(cuit => Registro(cuit)).ToArray());

        using var lector = new LectorPadronBinario(ruta);

        foreach (var cuit in cuits)
        {
            Assert.NotNull(lector.Buscar(cuit));
        }
    }

    [Fact]
    public void Escribir_OrdenaLosRegistrosPorCuitAscendente()
    {
        var cuits = new long[] { 30100100106, 20123456786, 33693450239, 27123456780, 23200000039 };
        var ruta = EscribirPadron(cuits.Select(cuit => Registro(cuit)).ToArray());

        var enElArchivo = LeerCuitsEnOrdenDeArchivo(ruta);

        Assert.Equal(cuits.OrderBy(cuit => cuit).ToArray(), enElArchivo);
    }

    [Fact]
    public void Buscar_EnUnPadronVacio_DevuelveNull()
    {
        var ruta = EscribirPadron();

        using var lector = new LectorPadronBinario(ruta);

        Assert.Equal(0, lector.CantidadRegistros);
        Assert.Null(lector.Buscar(30100100106));
    }

    [Fact]
    public void Buscar_EnUnPadronDeUnSoloRegistro_LoEncuentra()
    {
        var ruta = EscribirPadron(Registro(30100100106));

        using var lector = new LectorPadronBinario(ruta);

        Assert.NotNull(lector.Buscar(30100100106));
        Assert.Null(lector.Buscar(30100100105));
    }

    /// <summary>Los bordes son donde más se equivoca una búsqueda binaria.</summary>
    [Fact]
    public void Buscar_EncuentraElPrimeroYElUltimoDelArchivo()
    {
        var cuits = Enumerable.Range(0, 100).Select(i => 20000000000L + i * 7).ToArray();
        var ruta = EscribirPadron(cuits.Select(cuit => Registro(cuit)).ToArray());

        using var lector = new LectorPadronBinario(ruta);

        Assert.NotNull(lector.Buscar(cuits.Min()));
        Assert.NotNull(lector.Buscar(cuits.Max()));
        Assert.Null(lector.Buscar(cuits.Min() - 1));
        Assert.Null(lector.Buscar(cuits.Max() + 1));
    }

    /// <summary>
    /// El Campo 7 se guarda normalizado: la posición 0 es la jurisdicción 901 y
    /// la 23 la 924. Si se diera vuelta mal, el estado aparecería en la
    /// jurisdicción espejada.
    /// </summary>
    [Theory]
    [InlineData(901)]
    [InlineData(902)]
    [InlineData(912)]
    [InlineData(923)]
    [InlineData(924)]
    public void Buscar_DevuelveElEstadoEnLaJurisdiccionCorrecta(int codigoJurisdiccion)
    {
        var ruta = EscribirPadron(Registro(30100100106, campo7: Campo7Con(codigoJurisdiccion, '3')));

        using var lector = new LectorPadronBinario(ruta);
        var contribuyente = lector.Buscar(30100100106)!;

        Assert.Equal('3', contribuyente.EstadoDeJurisdiccion(codigoJurisdiccion));

        foreach (var otra in Enumerable.Range(Jurisdicciones.CodigoPrimera, Jurisdicciones.Cantidad)
                     .Where(codigo => codigo != codigoJurisdiccion))
        {
            Assert.Equal('1', contribuyente.EstadoDeJurisdiccion(otra));
        }
    }

    [Fact]
    public void EstadoDeJurisdiccion_ConUnCodigoFueraDeRango_Falla()
    {
        var ruta = EscribirPadron(Registro(30100100106));

        using var lector = new LectorPadronBinario(ruta);
        var contribuyente = lector.Buscar(30100100106)!;

        Assert.Throws<ArgumentOutOfRangeException>(() => contribuyente.EstadoDeJurisdiccion(900));
        Assert.Throws<ArgumentOutOfRangeException>(() => contribuyente.EstadoDeJurisdiccion(925));
    }

    [Fact]
    public void Escribir_ConElMismoCuitDosVeces_Falla()
    {
        var escritor = new EscritorPadronBinario(Periodo);
        escritor.Agregar(Registro(30100100106));
        escritor.Agregar(Registro(20123456786));
        escritor.Agregar(Registro(30100100106));

        using var archivo = new MemoryStream();
        var excepcion = Assert.Throws<CuitDuplicadoException>(() => escritor.Escribir(archivo));

        Assert.Equal(30100100106L, excepcion.Cuit);
    }

    /// <summary>El escritor arranca con capacidad chica: tiene que crecer solo.</summary>
    [Fact]
    public void Escribir_ConMasRegistrosQueLaCapacidadInicial_LosConservaATodos()
    {
        var escritor = new EscritorPadronBinario(Periodo, capacidadInicial: 2);
        var cuits = Enumerable.Range(0, 50).Select(i => 20000000000L + i).ToArray();
        foreach (var cuit in cuits)
        {
            escritor.Agregar(Registro(cuit));
        }

        var ruta = RutaNueva();
        using (var archivo = File.Create(ruta))
        {
            Assert.Equal(50, escritor.Escribir(archivo));
        }

        using var lector = new LectorPadronBinario(ruta);
        Assert.Equal(50, lector.CantidadRegistros);
        Assert.All(cuits, cuit => Assert.NotNull(lector.Buscar(cuit)));
    }

    [Fact]
    public void Lector_ConUnArchivoQueNoEsDePadron_Falla()
    {
        var ruta = RutaNueva();
        File.WriteAllBytes(ruta, new byte[FormatoPadronBinario.LargoEncabezado]);

        var excepcion = Assert.Throws<ArchivoPadronCorruptoException>(() => new LectorPadronBinario(ruta));

        Assert.Contains("no es un archivo de padrón", excepcion.Message);
    }

    [Fact]
    public void Lector_ConUnArchivoMasCortoQueElEncabezado_Falla()
    {
        var ruta = RutaNueva();
        File.WriteAllBytes(ruta, [1, 2, 3]);

        Assert.Throws<ArchivoPadronCorruptoException>(() => new LectorPadronBinario(ruta));
    }

    [Fact]
    public void Lector_ConUnaVersionDeFormatoDistinta_Falla()
    {
        var ruta = EscribirPadron(Registro(30100100106));
        var bytes = File.ReadAllBytes(ruta);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(FormatoPadronBinario.OffsetVersion), 99);
        File.WriteAllBytes(ruta, bytes);

        var excepcion = Assert.Throws<ArchivoPadronCorruptoException>(() => new LectorPadronBinario(ruta));

        Assert.Contains("versión de formato 99", excepcion.Message);
    }

    /// <summary>Un archivo truncado no puede pasar por bueno.</summary>
    [Fact]
    public void Lector_ConUnArchivoTruncado_Falla()
    {
        var ruta = EscribirPadron(Registro(30100100106), Registro(20123456786));
        var bytes = File.ReadAllBytes(ruta);
        File.WriteAllBytes(ruta, bytes[..^5]);

        var excepcion = Assert.Throws<ArchivoPadronCorruptoException>(() => new LectorPadronBinario(ruta));

        Assert.Contains("declara 2 registros", excepcion.Message);
    }

    private static long[] LeerCuitsEnOrdenDeArchivo(string ruta)
    {
        var bytes = File.ReadAllBytes(ruta);
        var cantidad = BinaryPrimitives.ReadInt32LittleEndian(
            bytes.AsSpan(FormatoPadronBinario.OffsetCantidadRegistros));

        return Enumerable.Range(0, cantidad)
            .Select(indice => BinaryPrimitives.ReadInt64LittleEndian(
                bytes.AsSpan((int)FormatoPadronBinario.OffsetDeRegistro(indice) + FormatoPadronBinario.OffsetCuit)))
            .ToArray();
    }
}
