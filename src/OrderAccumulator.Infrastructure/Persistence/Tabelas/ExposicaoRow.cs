using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Infrastructure.Persistence.Tabelas;

/// <summary>Uma linha da tabela "exposicoes". Só dados: as regras ficam no domínio.</summary>
public sealed class ExposicaoRow
{
    public Ativo Ativo { get; set; }
    public decimal Valor { get; set; }
}