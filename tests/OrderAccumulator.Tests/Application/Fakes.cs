using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.Exposicoes;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Tests.Application;

/// <summary>
/// Um "banco" de mentira: separa o que está confirmado do que está pendente na transação,
/// e só deixa uma transação aberta por vez (imitando o lock de linha do Postgres).
/// </summary>
internal sealed class BancoFake
{
    public Dictionary<Ativo, decimal> Saldos { get; } = [];
    public Dictionary<Guid, OrdemAceita> Aceitas { get; } = [];
    public List<IDomainEvent> Outbox { get; } = [];
    public bool SimularFalhaNoCommit { get; set; }
    public int Commits { get; private set; }

    internal List<Action> Pendentes { get; } = [];
    internal SemaphoreSlim Trava { get; } = new(1, 1);

    internal void Confirmar()
    {
        if (SimularFalhaNoCommit)
            throw new InvalidOperationException("Postgres fora do ar (simulado).");
        Pendentes.ForEach(gravar => gravar());
        Pendentes.Clear();
        Commits++;
    }
}

internal sealed class UnitOfWorkFake(BancoFake banco) : IUnitOfWork
{
    public async Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct = default)
    {
        await banco.Trava.WaitAsync(ct);
        return new TransacaoFake(banco);
    }

    private sealed class TransacaoFake(BancoFake banco) : ITransacao
    {
        public async Task ConfirmarAsync(CancellationToken ct = default)
        {
            await Task.Yield(); // devolve a thread, como um banco de verdade faria
            banco.Confirmar();
        }

        public ValueTask DisposeAsync()
        {
            banco.Pendentes.Clear(); // o que não foi confirmado é desfeito
            banco.Trava.Release();
            return ValueTask.CompletedTask;
        }
    }
}

internal sealed class RepositorioFake(BancoFake banco) : IExposicaoRepository
{
    public Task<ExposicaoAtivo> ObterAsync(Ativo ativo, CancellationToken ct = default) =>
        Task.FromResult(banco.Saldos.TryGetValue(ativo, out var valor)
            ? ExposicaoAtivo.Reconstituir(ativo, valor)
            : ExposicaoAtivo.Criar(ativo));

    public Task<ExposicaoAtivo> ObterParaAtualizarAsync(Ativo ativo, CancellationToken ct = default) =>
        ObterAsync(ativo, ct);

    public Task SalvarAsync(ExposicaoAtivo exposicao, CancellationToken ct = default)
    {
        var (ativo, valor) = (exposicao.Ativo, exposicao.Valor);
        banco.Pendentes.Add(() => banco.Saldos[ativo] = valor);
        return Task.CompletedTask;
    }
}

internal sealed class OrdensAceitasFake(BancoFake banco) : IOrdensAceitas
{
    public Task<OrdemAceita?> ObterAsync(Guid ordemId, CancellationToken ct = default) =>
        Task.FromResult(banco.Aceitas.GetValueOrDefault(ordemId));

    public Task RegistrarAsync(OrdemAceita evento, CancellationToken ct = default)
    {
        banco.Pendentes.Add(() => banco.Aceitas.TryAdd(evento.OrdemId, evento));
        return Task.CompletedTask;
    }
}

internal sealed class OutboxFake(BancoFake banco) : IOutbox
{
    public Task AdicionarAsync(IDomainEvent evento, CancellationToken ct = default)
    {
        banco.Pendentes.Add(() => banco.Outbox.Add(evento));
        return Task.CompletedTask;
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