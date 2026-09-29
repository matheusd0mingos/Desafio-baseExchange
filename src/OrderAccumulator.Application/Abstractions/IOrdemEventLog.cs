using OrderAccumulator.Domain.Exposicoes.Events;

namespace OrderAccumulator.Application.Abstractions;

/// <summary>
/// Log durável das ordens aceitas: a fonte da verdade. {e método para verificar com consolidado de exposições, para detectar divergências.}
/// AppendAsync só retorna depois que o registro está garantido;
/// se lançar exceção, a ordem NÃO foi registrada.
/// </summary>
public interface IOrdemEventLog
{
    Task AppendAsync(OrdemAceita evento, CancellationToken ct = default);

    IAsyncEnumerable<OrdemAceita> LerTodosAsync(CancellationToken ct = default);
}