namespace OrderAccumulator.Infrastructure.Messaging;

public static class Topicos
{
    public const string OrdensAceitas = "ordens-aceitas";
    public const string OrdensRejeitadas = "ordens-rejeitadas";

    public static string Para(string tipoDoEvento) => tipoDoEvento switch
    {
        "OrdemAceita" => OrdensAceitas,
        "OrdemRejeitada" => OrdensRejeitadas,
        _ => throw new InvalidOperationException($"Evento sem tópico definido: {tipoDoEvento}")
    };
}