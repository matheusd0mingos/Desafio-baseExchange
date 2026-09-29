using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Application.Abstractions;

/// <summary>
/// Caixa de saída: grava o evento na MESMA transação da ordem.
/// A publicação no Kafka acontece depois, por um serviço em segundo plano.
/// </summary>
public interface IOutbox
{
    Task AdicionarAsync(IDomainEvent evento, CancellationToken ct = default);
}