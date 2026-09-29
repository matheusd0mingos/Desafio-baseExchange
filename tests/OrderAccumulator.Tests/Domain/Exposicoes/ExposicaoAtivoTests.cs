using OrderAccumulator.Domain.Exposicoes;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.Ordens.Strategies;

namespace OrderAccumulator.Tests.Domain.Exposicoes;

public class ExposicaoAtivoTests
{
    private readonly LadoStrategyFactory _factory =
        new([new CompraStrategy(), new VendaStrategy()]);

    private Registro Registrar(ExposicaoAtivo exposicao, Lado lado, int quantidade, decimal preco)
    {
        var ordem = OrdemBuilder.Criar(exposicao.Ativo, lado, quantidade, preco);
        var resultado = exposicao.Registrar(ordem, _factory.Obter(lado));
        return new Registro(resultado.IsSuccess, resultado.IsFailure ? resultado.Error.Code : null);
    }

    private sealed record Registro(bool Sucesso, string? CodigoErro);

    [Fact]
    public void Compra_DeveAumentarExposicao()
    {
        var exposicao = ExposicaoAtivo.Criar(Ativo.PETR4);

        Registrar(exposicao, Lado.Compra, 584, 54.87m);

        Assert.Equal(32_044.08m, exposicao.Valor);
    }

    [Fact]
    public void Venda_DeveDiminuirExposicao()
    {
        var exposicao = ExposicaoAtivo.Criar(Ativo.PETR4);

        Registrar(exposicao, Lado.Venda, 100, 10m);

        Assert.Equal(-1_000m, exposicao.Valor);
    }

    [Fact]
    public void ExatamenteNoLimite_DeveSerAceita()
    {
        var exposicao = ExposicaoAtivo.Criar(Ativo.PETR4);

        var resultado = Registrar(exposicao, Lado.Compra, 10_000, 100m);

        Assert.True(resultado.Sucesso);
        Assert.Equal(1_000_000m, exposicao.Valor);
    }

    [Fact]
    public void UmCentavoAcimaDoLimite_DeveSerRejeitadaSemAlterarExposicao()
    {
        var exposicao = ExposicaoAtivo.Criar(Ativo.PETR4);
        Registrar(exposicao, Lado.Compra, 10_000, 100m);

        var resultado = Registrar(exposicao, Lado.Compra, 1, 0.01m);

        Assert.False(resultado.Sucesso);
        Assert.Equal("Exposicao.LimiteExcedido", resultado.CodigoErro);
        Assert.Equal(1_000_000m, exposicao.Valor);
    }

    [Fact]
    public void LimiteNegativo_TambemDeveRejeitar()
    {
        var exposicao = ExposicaoAtivo.Criar(Ativo.VALE3);
        Registrar(exposicao, Lado.Venda, 10_000, 100m);

        var resultado = Registrar(exposicao, Lado.Venda, 1, 0.01m);

        Assert.False(resultado.Sucesso);
        Assert.Equal(-1_000_000m, exposicao.Valor);
    }

    [Fact]
    public void NoLimite_VendaDeveSerAceitaPorqueReduzExposicao()
    {
        var exposicao = ExposicaoAtivo.Criar(Ativo.PETR4);
        Registrar(exposicao, Lado.Compra, 10_000, 100m);

        var resultado = Registrar(exposicao, Lado.Venda, 1_000, 100m);

        Assert.True(resultado.Sucesso);
        Assert.Equal(900_000m, exposicao.Valor);
    }

    [Fact]
    public void Aceite_DeveDispararOrdemAceita()
    {
        var exposicao = ExposicaoAtivo.Criar(Ativo.PETR4);

        Registrar(exposicao, Lado.Compra, 10, 10m);

        var evento = Assert.IsType<OrdemAceita>(Assert.Single(exposicao.DomainEvents));
        Assert.Equal(100m, evento.ExposicaoResultante);
    }

    [Fact]
    public void Rejeicao_DeveDispararOrdemRejeitadaComExposicaoInalterada()
    {
        var exposicao = ExposicaoAtivo.Reconstituir(Ativo.PETR4, 999_999m);

        Registrar(exposicao, Lado.Compra, 1, 10m);

        var evento = Assert.IsType<OrdemRejeitada>(Assert.Single(exposicao.DomainEvents));
        Assert.Equal(999_999m, evento.ExposicaoAtual);
    }

    [Fact]
    public void AtivosSaoIndependentes()
    {
        var petr = ExposicaoAtivo.Criar(Ativo.PETR4);
        var vale = ExposicaoAtivo.Criar(Ativo.VALE3);

        Registrar(petr, Lado.Compra, 10_000, 100m);
        var resultado = Registrar(vale, Lado.Compra, 10_000, 100m);

        Assert.True(resultado.Sucesso);
    }

    [Fact]
    public void OrdemDeOutroAtivo_DeveLancarExcecao()
    {
        var exposicao = ExposicaoAtivo.Criar(Ativo.PETR4);
        var ordem = OrdemBuilder.Criar(Ativo.VALE3, Lado.Compra, 1, 1m);

        Assert.Throws<InvalidOperationException>(() =>
            exposicao.Registrar(ordem, _factory.Obter(Lado.Compra)));
    }
}