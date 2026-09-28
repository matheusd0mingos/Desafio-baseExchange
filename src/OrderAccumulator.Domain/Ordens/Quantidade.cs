using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Domain.Ordens;

/// <summary>Value Object: inteiro positivo menor que 100.000. Impossível existir inválido.</summary>
public sealed record Quantidade
{
    public const int Maximo = 100_000; // exclusivo

    private Quantidade(int valor) => Valor = valor;

    public int Valor { get; }

    public static Result<Quantidade> Criar(int valor)
    {
        if (valor <= 0)
            return Result.Failure<Quantidade>(OrdemErrors.QuantidadeNaoPositiva);

        if (valor >= Maximo)
            return Result.Failure<Quantidade>(OrdemErrors.QuantidadeAcimaDoLimite);

        return Result.Success(new Quantidade(valor));
    }
}