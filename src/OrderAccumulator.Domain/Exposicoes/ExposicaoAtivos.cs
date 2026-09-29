using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.Ordens.Strategies;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Domain.Exposicoes;

public sealed class ExposicaoAtivo : AggregateRoot<Ativo>
{
    public const decimal Limite = 1_000_000m; // inclusivo: exatamente 1 milhão é aceito

    private ExposicaoAtivo(Ativo ativo, decimal valor) : base(ativo) => Valor = valor;

    public Ativo Ativo => Id;
    public decimal Valor { get; private set; }

    public static ExposicaoAtivo Criar(Ativo ativo) => new(ativo, 0m);

    public static ExposicaoAtivo Reconstituir(Ativo ativo, decimal valor) => new(ativo, valor);

    public Result<decimal> Registrar(Ordem ordem, ILadoStrategy estrategia)
    {
        ArgumentNullException.ThrowIfNull(ordem);
        ArgumentNullException.ThrowIfNull(estrategia);

        if (ordem.Ativo != Ativo)
            throw new InvalidOperationException($"Ordem de {ordem.Ativo} enviada à exposição de {Ativo}.");
        if (estrategia.Lado != ordem.Lado)
            throw new InvalidOperationException($"Estratégia de {estrategia.Lado} usada em ordem de {ordem.Lado}.");

        var novoValor = Valor + estrategia.CalcularImpacto(ordem);

        if (Math.Abs(novoValor) > Limite)
        {
            var erro = ExposicaoErrors.LimiteExcedido(Ativo, novoValor);
            Raise(new OrdemRejeitada(ordem.Id, Ativo, ordem.Lado, ordem.Quantidade.Valor,
                ordem.Preco.Valor, Valor, erro.Message, DateTimeOffset.UtcNow));
            return Result.Failure<decimal>(erro);
        }

        Valor = novoValor;
        Raise(new OrdemAceita(ordem.Id, Ativo, ordem.Lado, ordem.Quantidade.Valor,
            ordem.Preco.Valor, Valor, DateTimeOffset.UtcNow));
        return Result.Success(Valor);
    }
}