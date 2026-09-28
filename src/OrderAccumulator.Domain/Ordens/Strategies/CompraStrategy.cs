namespace OrderAccumulator.Domain.Ordens.Strategies;

/// <summary>Compra aumenta a exposição.</summary>
public sealed class CompraStrategy : ILadoStrategy
{
    public Lado Lado => Lado.Compra;

    public decimal CalcularImpacto(Ordem ordem) => ordem.ValorFinanceiro;
}