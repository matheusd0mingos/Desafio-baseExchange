using OrderAccumulator.Domain.Exposicoes.Events;

namespace OrderAccumulator.Application.Abstractions;

/// <summary>
/// Registro de ordens já aceitas, para idempotência:
/// o reenvio da mesma ordem devolve a resposta original em vez de contar duas vezes.
/// </summary>
public interface IOrdensAceitas
{
    Task<OrdemAceita?> ObterAsync(Guid ordemId, CancellationToken ct = default);

    Task RegistrarAsync(OrdemAceita evento, CancellationToken ct = default);
}