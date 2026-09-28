using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Tests.Domain.Ordens;

public class QuantidadeTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(584)]
    [InlineData(99_999)]
    public void Criar_ComValorValido_DeveTerSucesso(int valor)
    {
        // Act
        var resultado = Quantidade.Criar(valor);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(valor, resultado.Value.Valor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Criar_ComValorNaoPositivo_DeveFalhar(int valor)
    {
        var resultado = Quantidade.Criar(valor);

        Assert.Equal(OrdemErrors.QuantidadeNaoPositiva, resultado.Error);
    }

    [Theory]
    [InlineData(100_000)]
    [InlineData(150_000)]
    public void Criar_ComValorMaiorOuIgualACemMil_DeveFalhar(int valor)
    {
        var resultado = Quantidade.Criar(valor);

        Assert.Equal(OrdemErrors.QuantidadeAcimaDoLimite, resultado.Error);
    }
}