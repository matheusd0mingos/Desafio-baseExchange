using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Domain.Exposicoes.Events;

public sealed record OrdemAceita(
    Guid OrdemId,
    Ativo Ativo,
    Lado Lado,
    int Quantidade,
    decimal Preco,
    decimal ExposicaoResultante,
    DateTimeOffset OccurredOn) : IDomainEvent;