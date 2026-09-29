using System.Collections.Concurrent;
using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Application.Concorrencia;

/// <summary>
/// Uma fila por ativo: ordens do mesmo ativo passam uma de cada vez;
/// ativos diferentes andam em paralelo. Vale para uma instância da API.
/// </summary>
public sealed class AtivoLocks
{
    private readonly ConcurrentDictionary<Ativo, SemaphoreSlim> _semaforos = new();

    public async Task<IDisposable> TrancarAsync(Ativo ativo, CancellationToken ct = default)
    {
        var semaforo = _semaforos.GetOrAdd(ativo, _ => new SemaphoreSlim(1, 1));
        await semaforo.WaitAsync(ct);
        return new Chave(semaforo);
    }

    private sealed class Chave(SemaphoreSlim semaforo) : IDisposable
    {
        public void Dispose() => semaforo.Release();
    }
}