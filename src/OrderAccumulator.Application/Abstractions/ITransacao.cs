namespace OrderAccumulator.Application.Abstractions;

/// <summary>
/// Uma transação aberta. ConfirmarAsync grava tudo de uma vez.
/// Se for descartada (DisposeAsync) sem confirmar, tudo é desfeito.
/// </summary>
public interface ITransacao : IAsyncDisposable
{
    Task ConfirmarAsync(CancellationToken ct = default);
}