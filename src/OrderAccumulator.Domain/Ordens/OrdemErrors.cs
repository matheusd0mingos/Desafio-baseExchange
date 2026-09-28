using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Domain.Ordens;

/// <summary>Catálogo de erros de validação da ordem.</summary>
public static class OrdemErrors
{
    public static readonly Error QuantidadeNaoPositiva =
        new("Ordem.QuantidadeNaoPositiva", "a quantidade deve ser maior que zero.");

    public static readonly Error QuantidadeAcimaDoLimite =
        new("Ordem.QuantidadeAcimaDoLimite", "a quantidade deve ser menor que 100.000.");

    public static readonly Error PrecoNaoPositivo =
    new("Ordem.PrecoNaoPositivo", "o preço deve ser maior que zero.");

    public static readonly Error PrecoAcimaDoLimite =
        new("Ordem.PrecoAcimaDoLimite", "o preço deve ser menor que R$ 1.000,00.");

    public static readonly Error PrecoForaDoTick =
        new("Ordem.PrecoForaDoTick", "o preço deve ser múltiplo de R$ 0,01.");
}