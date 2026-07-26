using Sircip.Server.Padron;

namespace Sircip.Test;

/// <summary>Set de alícuotas del Campo 6 (Anexo A, nota 2).</summary>
public class SetAlicuotasTests
{
    [Theory]
    [InlineData('A', 0.00)]
    [InlineData('B', 0.01)]
    [InlineData('C', 0.05)]
    [InlineData('D', 0.10)]
    [InlineData('E', 0.20)]
    [InlineData('F', 0.30)]
    [InlineData('G', 0.40)]
    [InlineData('H', 0.50)]
    [InlineData('I', 0.60)]
    [InlineData('J', 0.70)]
    [InlineData('K', 0.80)]
    [InlineData('L', 1.00)]
    [InlineData('M', 1.20)]
    [InlineData('N', 1.40)]
    [InlineData('O', 1.50)]
    [InlineData('P', 1.60)]
    [InlineData('Q', 1.80)]
    [InlineData('R', 2.00)]
    [InlineData('S', 2.50)]
    [InlineData('T', 3.00)]
    [InlineData('U', 3.50)]
    [InlineData('V', 4.00)]
    [InlineData('W', 4.50)]
    [InlineData('X', 5.00)]
    public void ObtenerPorcentaje_DevuelveElPorcentajeDelAnexoA(char letra, double porcentajeEsperado)
    {
        Assert.True(SetAlicuotas.EsLetraValida(letra));
        Assert.Equal((decimal)porcentajeEsperado, SetAlicuotas.ObtenerPorcentaje(letra));
    }

    [Theory]
    [InlineData('Y')]
    [InlineData('Z')]
    [InlineData('a')]
    [InlineData('1')]
    [InlineData(' ')]
    public void EsLetraValida_ConUnaLetraFueraDelSet_DevuelveFalse(char letra)
    {
        Assert.False(SetAlicuotas.EsLetraValida(letra));
        Assert.Throws<ArgumentOutOfRangeException>(() => SetAlicuotas.ObtenerPorcentaje(letra));
    }
}
