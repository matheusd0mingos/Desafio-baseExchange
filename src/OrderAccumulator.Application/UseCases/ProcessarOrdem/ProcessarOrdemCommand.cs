using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Application.UseCases.ProcessarOrdem;

/// <summary>Pedido de processamento, ainda NÃO validado. Quem valida é o domínio (Ordem.Criar).</summary>
public sealed record ProcessarOrdemCommand(
    Guid OrdemId,
    Ativo Ativo,
    Lado Lado,
    int Quantidade,
    decimal Preco);