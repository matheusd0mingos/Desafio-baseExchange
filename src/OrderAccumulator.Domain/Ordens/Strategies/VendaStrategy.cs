namespace OrderAccumulator.Domain.Ordens.Strategies;

/// <summary>Venda diminui a exposição.</summary>
public sealed class VendaStrategy : ILadoStrategy
{
    public Lado Lado => Lado.Venda;

    public decimal CalcularImpacto(Ordem ordem) => -ordem.ValorFinanceiro;
}