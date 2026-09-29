namespace OrderAccumulator.Domain.SharedKernel;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}