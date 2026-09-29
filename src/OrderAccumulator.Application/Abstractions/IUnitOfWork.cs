namespace OrderAccumulator.Application.Abstractions;

/// <summary>Abre uma transação: tudo que for gravado dentro dela é confirmado junto, ou nada é.</summary>
public interface IUnitOfWork
{
    Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct = default);
}