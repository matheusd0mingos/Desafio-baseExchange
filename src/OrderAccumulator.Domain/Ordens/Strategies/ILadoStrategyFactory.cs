namespace OrderAccumulator.Domain.Ordens.Strategies;

public interface ILadoStrategyFactory
{
    ILadoStrategy Obter(Lado lado);
}