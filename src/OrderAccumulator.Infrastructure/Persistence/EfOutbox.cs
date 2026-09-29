using System.Text.Json;
using System.Text.Json.Serialization;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.SharedKernel;
using OrderAccumulator.Infrastructure.Persistence.Tabelas;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class EfOutbox(OrderAccumulatorDbContext db) : IOutbox
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task AdicionarAsync(IDomainEvent evento, CancellationToken ct = default)
    {
        db.Outbox.Add(new OutboxMensagem
        {
            Id = Guid.NewGuid(),
            Tipo = evento.GetType().Name,
            Chave = ChaveDeParticao(evento),
            Conteudo = JsonSerializer.Serialize(evento, evento.GetType(), Json),
            CriadaEm = evento.OccurredOn
        });

        return Task.CompletedTask;
    }

    // A chave do Kafka é o ativo: eventos do mesmo ativo caem na mesma partição, em ordem.
    private static string ChaveDeParticao(IDomainEvent evento) => evento switch
    {
        OrdemAceita aceita => aceita.Ativo.ToString(),
        OrdemRejeitada rejeitada => rejeitada.Ativo.ToString(),
        _ => evento.GetType().Name
    };
}