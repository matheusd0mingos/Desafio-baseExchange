using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Domain.Exposicoes.Events;

public sealed record OrdemRejeitada(
    Guid OrdemId,
    Ativo Ativo,
    Lado Lado,
    int Quantidade,
    decimal Preco,
    decimal ExposicaoAtual,
    string Motivo,
    DateTimeOffset OccurredOn) : IDomainEvent;