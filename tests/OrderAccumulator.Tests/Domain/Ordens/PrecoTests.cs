using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Tests.Domain.Ordens;

public class PrecoTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(54.87)]
    [InlineData(999.99)]
    public void Criar_ComValorValido_DeveTerSucesso(double valor)
    {
        var resultado = Preco.Criar((decimal)valor);

        Assert.True(resultado.IsSuccess);
        Assert.Equal((decimal)valor, resultado.Value.Valor);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-10.5)]
    public void Criar_ComValorNaoPositivo_DeveFalhar(double valor)
    {
        var resultado = Preco.Criar((decimal)valor);

        Assert.Equal(OrdemErrors.PrecoNaoPositivo, resultado.Error);
    }

    [Theory]
    [InlineData(1000.0)]
    [InlineData(1500.5)]
    public void Criar_ComValorMaiorOuIgualAMil_DeveFalhar(double valor)
    {
        var resultado = Preco.Criar((decimal)valor);

        Assert.Equal(OrdemErrors.PrecoAcimaDoLimite, resultado.Error);
    }

    [Theory]
    [InlineData(10.001)]
    [InlineData(54.875)]
    public void Criar_ComValorForaDoTick_DeveFalhar(double valor)
    {
        var resultado = Preco.Criar((decimal)valor);

        Assert.Equal(OrdemErrors.PrecoForaDoTick, resultado.Error);
    }
}