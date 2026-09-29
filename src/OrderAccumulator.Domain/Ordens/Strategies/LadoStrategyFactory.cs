namespace OrderAccumulator.Domain.Ordens.Strategies;

public sealed class LadoStrategyFactory(IEnumerable<ILadoStrategy> estrategias) : ILadoStrategyFactory
{
    private readonly Dictionary<Lado, ILadoStrategy> _estrategias = estrategias.ToDictionary(e => e.Lado);

    public ILadoStrategy Obter(Lado lado) =>
        _estrategias.GetValueOrDefault(lado)
        ?? throw new InvalidOperationException($"Nenhuma estratégia registrada para o lado {lado}.");
}