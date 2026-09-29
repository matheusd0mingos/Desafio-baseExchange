using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Infrastructure.Persistence.Tabelas;

/// <summary>Uma linha da tabela "ordens_aceitas". A chave é o Id da ordem: garante idempotência.</summary>
public sealed class OrdemAceitaRow
{
    public Guid OrdemId { get; set; }
    public Ativo Ativo { get; set; }
    public Lado Lado { get; set; }
    public int Quantidade { get; set; }
    public decimal Preco { get; set; }
    public decimal ExposicaoResultante { get; set; }
    public DateTimeOffset OccurredOn { get; set; }
}