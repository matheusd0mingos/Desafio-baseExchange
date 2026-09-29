using Microsoft.Extensions.Logging;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Application.Concorrencia;
using OrderAccumulator.Application.Observers;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.Ordens.Strategies;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Application.UseCases.ProcessarOrdem;

public sealed class ProcessarOrdemUseCase(
    IExposicaoRepository exposicoes,
    IOrdensAceitas ordensAceitas,
    IOrdemEventLog eventLog,
    ILadoStrategyFactory estrategias,
    AtivoLocks locks,
    OrderEventNotifier notifier,
    ILogger<ProcessarOrdemUseCase> logger)
{
    public async Task<ProcessarOrdemResult> ExecutarAsync(ProcessarOrdemCommand command, CancellationToken ct = default)
    {
        // 1. O domínio valida montando a Ordem
        var ordemResult = Ordem.Criar(command.OrdemId, command.Ativo, command.Lado, command.Quantidade, command.Preco);
        if (ordemResult.IsFailure)
            return ProcessarOrdemResult.Rejeitada(await ExposicaoAtualAsync(command.Ativo, ct), ordemResult.Error);

        var ordem = ordemResult.Value;

        // 2. Uma ordem por vez neste ativo
        ProcessarOrdemResult resultado;
        IDomainEvent[] eventos;
        using (await locks.TrancarAsync(ordem.Ativo, ct))
        {
            (resultado, eventos) = await ProcessarComAtivoTrancadoAsync(ordem, ct);
        }

        // 7. Avisa os interessados fora do lock, para não segurar a fila
        await notifier.NotificarAsync(eventos, ct);
        return resultado;
    }

    private async Task<(ProcessarOrdemResult, IDomainEvent[])> ProcessarComAtivoTrancadoAsync(Ordem ordem, CancellationToken ct)
    {
        // 3. Idempotência: reenvio devolve a resposta original
        var jaAceita = await ordensAceitas.ObterAsync(ordem.Id, ct);
        if (jaAceita is not null)
            return (ProcessarOrdemResult.Aceita(jaAceita.ExposicaoResultante), []);

        // 4. Cópia da exposição + decisão do domínio
        var exposicao = await exposicoes.ObterAsync(ordem.Ativo, ct);
        var valorAnterior = exposicao.Valor;
        var decisao = exposicao.Registrar(ordem, estrategias.Obter(ordem.Lado));

        if (decisao.IsFailure)
            return (ProcessarOrdemResult.Rejeitada(valorAnterior, decisao.Error), [.. exposicao.DomainEvents]);

        var aceita = exposicao.DomainEvents.OfType<OrdemAceita>().Single();

        // 5. Primeiro o Kafka. Daqui em diante não cancelamos: é o ponto de não retorno.
        try
        {
            await eventLog.AppendAsync(aceita, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao gravar a ordem {OrdemId} no log durável.", ordem.Id);
            return (ProcessarOrdemResult.Rejeitada(valorAnterior, ProcessarOrdemErrors.LogIndisponivel), []);
        }

        // 6. Depois a RAM
        await exposicoes.SalvarAsync(exposicao, CancellationToken.None);
        await ordensAceitas.RegistrarAsync(aceita, CancellationToken.None);

        return (ProcessarOrdemResult.Aceita(exposicao.Valor), [.. exposicao.DomainEvents]);
    }

    private async Task<decimal> ExposicaoAtualAsync(Ativo ativo, CancellationToken ct) =>
        Enum.IsDefined(ativo) ? (await exposicoes.ObterAsync(ativo, ct)).Valor : 0m;
}