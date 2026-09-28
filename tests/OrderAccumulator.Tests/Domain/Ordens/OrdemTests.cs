using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Tests.Domain.Ordens;

public class OrdemTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveCalcularValorFinanceiro()
    {
        var ordem = OrdemBuilder.Criar(Ativo.PETR4, Lado.Compra, 584, 54.87m);

        Assert.Equal(32_044.08m, ordem.ValorFinanceiro);
    }

    [Fact]
    public void Criar_SemId_DeveFalhar()
    {
        var resultado = Ordem.Criar(Guid.Empty, Ativo.PETR4, Lado.Compra, 1, 1m);

        Assert.Equal(OrdemErrors.IdInvalido, resultado.Error);
    }

    [Fact]
    public void Criar_ComAtivoInexistente_DeveFalhar()
    {
        var resultado = Ordem.Criar(Guid.NewGuid(), (Ativo)99, Lado.Compra, 1, 1m);

        Assert.Equal(OrdemErrors.AtivoInvalido, resultado.Error);
    }

    [Fact]
    public void Criar_ComQuantidadeInvalida_DevePropagarErroDoValueObject()
    {
        var resultado = Ordem.Criar(Guid.NewGuid(), Ativo.PETR4, Lado.Compra, 0, 1m);

        Assert.Equal(OrdemErrors.QuantidadeNaoPositiva, resultado.Error);
    }
}