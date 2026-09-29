using Microsoft.Extensions.Logging;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Infrastructure.Observers;

public sealed class LogOrderObserver(ILogger<LogOrderObserver> logger) : IOrderEventObserver
{
    public Task OnEventAsync(IDomainEvent evento, CancellationToken ct = default)
    {
        switch (evento)
        {
            case OrdemAceita aceita:
                logger.LogInformation("Ordem {OrdemId} aceita: {Lado} {Quantidade} {Ativo} a {Preco}. Exposição: {Exposicao}.",
                    aceita.OrdemId, aceita.Lado, aceita.Quantidade, aceita.Ativo, aceita.Preco, aceita.ExposicaoResultante);
                break;

            case OrdemRejeitada rejeitada:
                logger.LogWarning("Ordem {OrdemId} rejeitada ({Ativo}): {Motivo}",
                    rejeitada.OrdemId, rejeitada.Ativo, rejeitada.Motivo);
                break;
        }

        return Task.CompletedTask;
    }
}