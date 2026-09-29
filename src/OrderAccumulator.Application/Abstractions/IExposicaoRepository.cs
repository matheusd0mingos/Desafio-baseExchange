using OrderAccumulator.Domain.Exposicoes;
using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Application.Abstractions;

public interface IExposicaoRepository
{
    /// <summary>Devolve sempre uma cópia. Ativo sem histórico volta zerado.</summary>
    Task<ExposicaoAtivo> ObterAsync(Ativo ativo, CancellationToken ct = default);

    Task SalvarAsync(ExposicaoAtivo exposicao, CancellationToken ct = default);
}