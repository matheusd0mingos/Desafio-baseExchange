using Microsoft.Extensions.Logging.Abstractions;
using OrderAccumulator.Application.Observers;
using OrderAccumulator.Application.UseCases.ProcessarOrdem;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.Ordens.Strategies;

namespace OrderAccumulator.Tests.Application;

public class ProcessarOrdemUseCaseTests
{
    private readonly BancoFake _banco = new();
    private readonly ObserverFake _observer = new();
    private readonly ProcessarOrdemUseCase _useCase;

    public ProcessarOrdemUseCaseTests()
    {
        _useCase = new ProcessarOrdemUseCase(
            new UnitOfWorkFake(_banco),
            new RepositorioFake(_banco),
            new OrdensAceitasFake(_banco),
            new OutboxFake(_banco),
            new LadoStrategyFactory([new CompraStrategy(), new VendaStrategy()]),
            new OrderEventNotifier([_observer], NullLogger<OrderEventNotifier>.Instance),
            NullLogger<ProcessarOrdemUseCase>.Instance);
    }

    private static ProcessarOrdemCommand Ordem(Lado lado, int quantidade, decimal preco, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Ativo.PETR4, lado, quantidade, preco);

    [Fact]
    public async Task OrdemValida_DeveSerAceita_EGravarSaldoEOutboxJuntos()
    {
        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 584, 54.87m));

        Assert.True(resultado.Sucesso);
        Assert.Equal(32_044.08m, resultado.ExposicaoAtual);
        Assert.Equal(32_044.08m, _banco.Saldos[Ativo.PETR4]);
        Assert.IsType<OrdemAceita>(Assert.Single(_banco.Outbox));
        Assert.Equal(1, _banco.Commits);
    }
    [Fact]
    public async Task MesmaChave_ComOrdemDiferente_DeveSerRecusada()
    {
        var id = Guid.NewGuid();
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m, id));

        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 99m, id));

        Assert.False(resultado.Sucesso);
        Assert.Equal(ProcessarOrdemErrors.ChaveReutilizada, resultado.Erro);
        Assert.Equal(1_000m, resultado.ExposicaoAtual);
        Assert.Single(_banco.Outbox);
    }

    [Fact]
    public async Task OrdemInvalida_DeveRejeitarSemAbrirTransacao()
    {
        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 0, 10m));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Ordem.QuantidadeNaoPositiva", resultado.Erro!.Code);
        Assert.Equal(0, _banco.Commits);
    }

    [Fact]
    public async Task LimiteExcedido_DeveRejeitar_SemMudarSaldo_MasAuditarNaOutbox()
    {
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 10_000, 100m));

        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 1, 0.01m));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Exposicao.LimiteExcedido", resultado.Erro!.Code);
        Assert.Equal(1_000_000m, resultado.ExposicaoAtual);
        Assert.Equal(1_000_000m, _banco.Saldos[Ativo.PETR4]);
        Assert.IsType<OrdemRejeitada>(_banco.Outbox.Last());
    }

    [Fact]
    public async Task Reenvio_ComMesmoId_DeveResponderIgual_SemGravarNada()
    {
        var id = Guid.NewGuid();
        var primeira = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m, id));

        var reenvio = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m, id));

        Assert.Equal(primeira, reenvio);
        Assert.Single(_banco.Outbox);
        Assert.Equal(1_000m, _banco.Saldos[Ativo.PETR4]);
    }

    [Fact]
    public async Task BancoForaDoAr_DeveFalharFechado_SemGravarNada()
    {
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m));
        _banco.SimularFalhaNoCommit = true;

        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m));

        Assert.False(resultado.Sucesso);
        Assert.Equal(ProcessarOrdemErrors.PersistenciaIndisponivel, resultado.Erro);
        Assert.Equal(1_000m, resultado.ExposicaoAtual);
        Assert.Equal(1_000m, _banco.Saldos[Ativo.PETR4]);
        Assert.Single(_banco.Outbox);
    }

    [Fact]
    public async Task Observers_SoRecebemDepoisDoCommit()
    {
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 10_000, 100m));
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 1, 1m));
        _banco.SimularFalhaNoCommit = true;
        await _useCase.ExecutarAsync(Ordem(Lado.Venda, 1, 1m));

        Assert.Collection(_observer.Recebidos,
            e => Assert.IsType<OrdemAceita>(e),
            e => Assert.IsType<OrdemRejeitada>(e));
    }

    [Fact]
    public async Task OrdensSimultaneas_NaoPodemFurarOLimite()
    {
        // 20 compras de R$ 100.000 ao mesmo tempo: só 10 cabem no limite de R$ 1 milhão.
        var tarefas = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => _useCase.ExecutarAsync(Ordem(Lado.Compra, 1_000, 100m))));

        var resultados = await Task.WhenAll(tarefas);

        Assert.Equal(10, resultados.Count(r => r.Sucesso));
        Assert.Equal(1_000_000m, _banco.Saldos[Ativo.PETR4]);
    }
}