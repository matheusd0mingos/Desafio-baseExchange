using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderAccumulator.Infrastructure.Persistence;

namespace OrderAccumulator.Infrastructure.Messaging;

/// <summary>
/// O mensageiro: a cada segundo, pega as mensagens pendentes da outbox,
/// publica no Kafka e marca como publicadas. Kafka fora do ar? Tenta de novo depois.
/// </summary>
public sealed class OutboxPublisher(
    IServiceScopeFactory scopes,
    IProducer<string, string> producer,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const int TamanhoDoLote = 100;
    private static readonly TimeSpan Intervalo = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalo);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PublicarLoteAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Kafka ou banco fora: nada se perde, as mensagens continuam pendentes.
                logger.LogWarning(ex, "Não foi possível publicar a outbox agora; nova tentativa em {Intervalo}.", Intervalo);
            }
        }
    }

    private async Task PublicarLoteAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderAccumulatorDbContext>();

        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        // SKIP LOCKED: se houver duas instâncias da API, cada uma pega linhas diferentes.
        var pendentes = await db.Outbox
            .FromSql($"""
                SELECT * FROM outbox
                WHERE publicada_em IS NULL
                ORDER BY criada_em
                LIMIT {TamanhoDoLote}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        if (pendentes.Count == 0)
            return;

        foreach (var mensagem in pendentes)
        {
            // Espera o "ok" do Kafka (acks=all). Se falhar, lança e nada é marcado.
            await producer.ProduceAsync(
                Topicos.Para(mensagem.Tipo),
                new Message<string, string> { Key = mensagem.Chave, Value = mensagem.Conteudo },
                ct);

            mensagem.PublicadaEm = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);

        logger.LogInformation("{Quantidade} mensagem(ns) da outbox publicada(s) no Kafka.", pendentes.Count);
    }
}