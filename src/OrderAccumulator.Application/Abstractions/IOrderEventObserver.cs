using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Application.Abstractions;

/// <summary>Interessado em eventos de domínio. Deve ser rápido: roda antes da resposta ao cliente.</summary>
public interface IOrderEventObserver
{
    Task OnEventAsync(IDomainEvent evento, CancellationToken ct = default);
}