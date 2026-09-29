using System.Collections.Concurrent;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.Exposicoes;
using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Infrastructure.Persistence;

/// <summary>
/// Guarda só o saldo (um número), nunca o objeto. Assim, cada ObterAsync
/// devolve uma exposição nova: mexer nela não afeta o oficial até o SalvarAsync.
/// </summary>
public sealed class ExposicaoEmMemoriaRepository : IExposicaoRepository
{
    private readonly ConcurrentDictionary<Ativo, decimal> _saldos = new();

    public Task<ExposicaoAtivo> ObterAsync(Ativo ativo, CancellationToken ct = default)
    {
        var exposicao = _saldos.TryGetValue(ativo, out var valor)
            ? ExposicaoAtivo.Reconstituir(ativo, valor)
            : ExposicaoAtivo.Criar(ativo);

        return Task.FromResult(exposicao);
    }

    public Task SalvarAsync(ExposicaoAtivo exposicao, CancellationToken ct = default)
    {
        _saldos[exposicao.Ativo] = exposicao.Valor;
        return Task.CompletedTask;
    }
}