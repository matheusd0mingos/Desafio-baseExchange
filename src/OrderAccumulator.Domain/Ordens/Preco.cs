using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Domain.Ordens;

/// <summary>Value Object: decimal positivo, múltiplo de 0,01 e menor que 1.000.</summary>
public sealed record Preco
{
    public const decimal Maximo = 1_000m; // exclusivo

    private Preco(decimal valor) => Valor = valor;

    public decimal Valor { get; }

    public static Result<Preco> Criar(decimal valor)
    {
        if (valor <= 0)
            return Result.Failure<Preco>(OrdemErrors.PrecoNaoPositivo);

        if (valor >= Maximo)
            return Result.Failure<Preco>(OrdemErrors.PrecoAcimaDoLimite);

        if (decimal.Round(valor, 2) != valor)
            return Result.Failure<Preco>(OrdemErrors.PrecoForaDoTick);

        return Result.Success(new Preco(valor));
    }
}
