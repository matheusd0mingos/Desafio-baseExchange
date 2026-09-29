using Microsoft.Extensions.Logging.Abstractions;
using OrderAccumulator.Application.Concorrencia;
using OrderAccumulator.Application.Observers;
using OrderAccumulator.Application.UseCases.ProcessarOrdem;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.Ordens.Strategies;

namespace OrderAccumulator.Tests.Application;

public class ProcessarOrdemUseCaseTests
{
    private readonly RepositorioFake _repositorio = new();
    private readonly EventLogFake _log = new();
    private readonly ObserverFake _observer = new();
    private readonly ProcessarOrdemUseCase _useCase;

    public ProcessarOrdemUseCaseTests()
    {
        _useCase = new ProcessarOrdemUseCase(
            _repositorio,
            new OrdensAceitasFake(),
            _log,
            new LadoStrategyFactory([new CompraStrategy(), new VendaStrategy()]),
            new AtivoLocks(),
            new OrderEventNotifier([_observer], NullLogger<OrderEventNotifier>.Instance),
            NullLogger<ProcessarOrdemUseCase>.Instance);
    }

    private static ProcessarOrdemCommand Ordem(Lado lado, int quantidade, decimal preco, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Ativo.PETR4, lado, quantidade, preco);

    [Fact]
    public async Task OrdemValida_DeveSerAceita_GravadaNoLog_ESalvaNaMemoria()
    {
        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 584, 54.87m));

        Assert.True(resultado.Sucesso);
        Assert.Equal(32_044.08m, resultado.ExposicaoAtual);
        Assert.Single(_log.Gravados);
        Assert.Equal(32_044.08m, _repositorio.Saldos[Ativo.PETR4]);
    }

    [Fact]
    public async Task OrdemInvalida_DeveRejeitarComErroDoDominio_SemGravar()
    {
        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 0, 10m));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Ordem.QuantidadeNaoPositiva", resultado.Erro!.Code);
        Assert.Empty(_log.Gravados);
    }

    [Fact]
    public async Task LimiteExcedido_DeveRejeitarComExposicaoAtual_SemGravarNemSalvar()
    {
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 10_000, 100m));

        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 1, 0.01m));

        Assert.False(resultado.Sucesso);
        Assert.Equal("Exposicao.LimiteExcedido", resultado.Erro!.Code);
        Assert.Equal(1_000_000m, resultado.ExposicaoAtual);
        Assert.Single(_log.Gravados);
        Assert.Equal(1_000_000m, _repositorio.Saldos[Ativo.PETR4]);
    }

    [Fact]
    public async Task Reenvio_ComMesmoId_DeveResponderIgual_SemContarDuasVezes()
    {
        var id = Guid.NewGuid();
        var primeira = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m, id));

        var reenvio = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m, id));

        Assert.Equal(primeira, reenvio);
        Assert.Single(_log.Gravados);
        Assert.Equal(1_000m, _repositorio.Saldos[Ativo.PETR4]);
    }

    [Fact]
    public async Task KafkaForaDoAr_DeveFalharFechado_ComSaldoIntacto()
    {
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m));
        _log.SimularFalha = true;

        var resultado = await _useCase.ExecutarAsync(Ordem(Lado.Compra, 100, 10m));

        Assert.False(resultado.Sucesso);
        Assert.Equal(ProcessarOrdemErrors.LogIndisponivel, resultado.Erro);
        Assert.Equal(1_000m, resultado.ExposicaoAtual);
        Assert.Equal(1_000m, _repositorio.Saldos[Ativo.PETR4]);
    }

    [Fact]
    public async Task Observers_DevemReceberAceitasERejeitadas()
    {
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 10_000, 100m));
        await _useCase.ExecutarAsync(Ordem(Lado.Compra, 1, 1m));

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
        Assert.Equal(1_000_000m, _repositorio.Saldos[Ativo.PETR4]);
    }
}