using Microsoft.Extensions.Logging;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Application.Observers;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.Ordens.Strategies;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Application.UseCases.ProcessarOrdem;

public sealed class ProcessarOrdemUseCase(
    IUnitOfWork unitOfWork,
    IExposicaoRepository exposicoes,
    IOrdensAceitas ordensAceitas,
    IOutbox outbox,
    ILadoStrategyFactory estrategias,
    OrderEventNotifier notifier,
    ILogger<ProcessarOrdemUseCase> logger)
{
    public async Task<ProcessarOrdemResult> ExecutarAsync(ProcessarOrdemCommand command, CancellationToken ct = default)
    {
        // 1. O domínio valida montando a Ordem
        var ordemResult = Ordem.Criar(command.OrdemId, command.Ativo, command.Lado, command.Quantidade, command.Preco);
        if (ordemResult.IsFailure)
            return ProcessarOrdemResult.Rejeitada(await ExposicaoAtualAsync(command.Ativo, ct), ordemResult.Error);

        // 2 a 6. Tudo numa transação
        var (resultado, eventos) = await ProcessarEmTransacaoAsync(ordemResult.Value, ct);

        // 7. Efeitos locais, só depois do commit
        await notifier.NotificarAsync(eventos, ct);
        return resultado;
    }

    private async Task<(ProcessarOrdemResult, IDomainEvent[])> ProcessarEmTransacaoAsync(Ordem ordem, CancellationToken ct)
    {
        var exposicaoConhecida = 0m;
        try
        {
            // 2. Abre a transação (descartada sem confirmar = tudo desfeito)
            await using var transacao = await unitOfWork.IniciarTransacaoAsync(ct);

            // 3. Lê e trava o ativo: uma ordem por vez, em qualquer instância da API
            var exposicao = await exposicoes.ObterParaAtualizarAsync(ordem.Ativo, ct);
            exposicaoConhecida = exposicao.Valor;

            // 4. Idempotência: reenvio devolve a resposta original, sem gravar nada
            var jaAceita = await ordensAceitas.ObterAsync(ordem.Id, ct);
            if (jaAceita is not null)
            {
                // Mesma chave com conteúdo diferente não é reenvio: é erro do cliente.
                var mesmaOrdem = jaAceita.Ativo == ordem.Ativo
                    && jaAceita.Lado == ordem.Lado
                    && jaAceita.Quantidade == ordem.Quantidade.Valor
                    && jaAceita.Preco == ordem.Preco.Valor;

                return mesmaOrdem
                    ? (ProcessarOrdemResult.Aceita(jaAceita.ExposicaoResultante), [])
                    : (ProcessarOrdemResult.Rejeitada(exposicaoConhecida, ProcessarOrdemErrors.ChaveReutilizada), []);
            }
            // 5. Decisão do domínio
            var decisao = exposicao.Registrar(ordem, estrategias.Obter(ordem.Lado));
            IDomainEvent[] eventos = [.. exposicao.DomainEvents];

            // 6. Grava estado + caixa de saída, e confirma tudo junto
            if (decisao.IsSuccess)
            {
                await exposicoes.SalvarAsync(exposicao, ct);
                await ordensAceitas.RegistrarAsync(eventos.OfType<OrdemAceita>().Single(), ct);
            }

            foreach (var evento in eventos)
                await outbox.AdicionarAsync(evento, ct);

            await transacao.ConfirmarAsync(CancellationToken.None); // ponto de não retorno

            var resultado = decisao.IsSuccess
                ? ProcessarOrdemResult.Aceita(exposicao.Valor)
                : ProcessarOrdemResult.Rejeitada(exposicaoConhecida, decisao.Error);

            return (resultado, eventos);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Falha de persistência ao processar a ordem {OrdemId}.", ordem.Id);
            return (ProcessarOrdemResult.Rejeitada(exposicaoConhecida, ProcessarOrdemErrors.PersistenciaIndisponivel), []);
        }
    }

    private async Task<decimal> ExposicaoAtualAsync(Ativo ativo, CancellationToken ct)
    {
        if (!Enum.IsDefined(ativo))
            return 0m;

        try
        {
            return (await exposicoes.ObterAsync(ativo, ct)).Valor;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Não foi possível ler a exposição de {Ativo}.", ativo);
            return 0m;
        }
    }
}