using System.Runtime.CompilerServices;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.Exposicoes;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Tests.Application;

/// <summary>Plugues falsos: cumprem os contratos das tomadas, guardando tudo em memória.</summary>
internal sealed class RepositorioFake : IExposicaoRepository
{
    public Dictionary<Ativo, decimal> Saldos { get; } = [];

    public Task<ExposicaoAtivo> ObterAsync(Ativo ativo, CancellationToken ct = default) =>
        Task.FromResult(Saldos.TryGetValue(ativo, out var valor)
            ? ExposicaoAtivo.Reconstituir(ativo, valor)
            : ExposicaoAtivo.Criar(ativo));

    public Task SalvarAsync(ExposicaoAtivo exposicao, CancellationToken ct = default)
    {
        Saldos[exposicao.Ativo] = exposicao.Valor;
        return Task.CompletedTask;
    }
}

internal sealed class OrdensAceitasFake : IOrdensAceitas
{
    private readonly Dictionary<Guid, OrdemAceita> _aceitas = [];

    public Task<OrdemAceita?> ObterAsync(Guid ordemId, CancellationToken ct = default) =>
        Task.FromResult(_aceitas.GetValueOrDefault(ordemId));

    public Task RegistrarAsync(OrdemAceita evento, CancellationToken ct = default)
    {
        _aceitas[evento.OrdemId] = evento;
        return Task.CompletedTask;
    }
}

internal sealed class EventLogFake : IOrdemEventLog
{
    public List<OrdemAceita> Gravados { get; } = [];
    public bool SimularFalha { get; set; }

    public async Task AppendAsync(OrdemAceita evento, CancellationToken ct = default)
    {
        await Task.Yield(); // devolve a thread, como um Kafka de verdade faria
        if (SimularFalha)
            throw new InvalidOperationException("Kafka fora do ar (simulado).");
        Gravados.Add(evento);
    }

    public async IAsyncEnumerable<OrdemAceita> LerTodosAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var evento in Gravados)
        {
            await Task.Yield();
            yield return evento;
        }
    }
}

internal sealed class ObserverFake : IOrderEventObserver
{
    public List<IDomainEvent> Recebidos { get; } = [];

    public Task OnEventAsync(IDomainEvent evento, CancellationToken ct = default)
    {
        Recebidos.Add(evento);
        return Task.CompletedTask;
    }
}