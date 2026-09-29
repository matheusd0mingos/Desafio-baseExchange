using Microsoft.Extensions.Logging;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Application.Observers;

public sealed class OrderEventNotifier(
    IEnumerable<IOrderEventObserver> observers,
    ILogger<OrderEventNotifier> logger)
{
    public async Task NotificarAsync(IEnumerable<IDomainEvent> eventos, CancellationToken ct = default)
    {
        foreach (var evento in eventos)
        {
            foreach (var observer in observers)
            {
                try
                {
                    await observer.OnEventAsync(evento, ct);
                }
                catch (Exception ex)
                {
                    // Um interessado com problema não derruba os outros nem a resposta.
                    logger.LogError(ex, "Observer {Observer} falhou ao processar {Evento}.",
                        observer.GetType().Name, evento.GetType().Name);
                }
            }
        }
    }
}