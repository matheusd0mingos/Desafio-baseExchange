using OrderAccumulator.Domain.Exposicoes;
using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Application.Abstractions;

public interface IExposicaoRepository
{
    /// <summary>Leitura simples, sem trava. Serve para respostas informativas.</summary>
    Task<ExposicaoAtivo> ObterAsync(Ativo ativo, CancellationToken ct = default);

    /// <summary>Lê e TRAVA a exposição do ativo até o fim da transação. Exige transação aberta.</summary>
    Task<ExposicaoAtivo> ObterParaAtualizarAsync(Ativo ativo, CancellationToken ct = default);

    Task SalvarAsync(ExposicaoAtivo exposicao, CancellationToken ct = default);
}