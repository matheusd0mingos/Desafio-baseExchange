using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Domain.Ordens;

/// <summary>
/// Entidade Ordem. O Id vem do cliente e garante idempotência
/// (o mesmo envio repetido não conta duas vezes).
/// </summary>
public sealed class Ordem : Entity<Guid>
{
    private Ordem(Guid id, Ativo ativo, Lado lado, Quantidade quantidade, Preco preco) : base(id)
    {
        Ativo = ativo;
        Lado = lado;
        Quantidade = quantidade;
        Preco = preco;
    }

    public Ativo Ativo { get; }
    public Lado Lado { get; }
    public Quantidade Quantidade { get; }
    public Preco Preco { get; }

    /// <summary>Preço × quantidade, sempre positivo. O sinal é decidido pela Strategy do lado.</summary>
    public decimal ValorFinanceiro => Preco.Valor * Quantidade.Valor;

    public static Result<Ordem> Criar(Guid id, Ativo ativo, Lado lado, int quantidade, decimal preco)
    {
        if (id == Guid.Empty)
            return Result.Failure<Ordem>(OrdemErrors.IdInvalido);

        if (!Enum.IsDefined(ativo))
            return Result.Failure<Ordem>(OrdemErrors.AtivoInvalido);

        if (!Enum.IsDefined(lado))
            return Result.Failure<Ordem>(OrdemErrors.LadoInvalido);

        var quantidadeResult = Quantidade.Criar(quantidade);
        if (quantidadeResult.IsFailure)
            return Result.Failure<Ordem>(quantidadeResult.Error);

        var precoResult = Preco.Criar(preco);
        if (precoResult.IsFailure)
            return Result.Failure<Ordem>(precoResult.Error);

        return Result.Success(new Ordem(id, ativo, lado, quantidadeResult.Value, precoResult.Value));
    }
}