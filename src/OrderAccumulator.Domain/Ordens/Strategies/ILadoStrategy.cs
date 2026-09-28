
namespace OrderAccumulator.Domain.Ordens.Strategies;

/// <summary>Strategy: cada lado sabe como a ordem impacta a exposição.</summary>
public interface ILadoStrategy
{
    Lado Lado { get; }

    decimal CalcularImpacto(Ordem ordem);
}