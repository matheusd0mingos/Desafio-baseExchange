using System.Text.Json;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.SharedKernel;
using OrderAccumulator.Infrastructure.Persistence.Tabelas;
using OrderExposure.Contracts.Eventos;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class EfOutbox(OrderAccumulatorDbContext db) : IOutbox
{
    public Task AdicionarAsync(IDomainEvent evento, CancellationToken ct = default)
    {
        var mensagem = ParaMensagem(evento);

        db.Outbox.Add(new OutboxMensagem
        {
            Id = Guid.NewGuid(),
            Tipo = mensagem.Evento,
            Chave = mensagem.Ativo, // chave do Kafka = ativo: mesma partição, mesma ordem
            Conteudo = JsonSerializer.Serialize(mensagem),
            CriadaEm = evento.OccurredOn
        });

        return Task.CompletedTask;
    }

    // Tradução: evento de DOMÍNIO (interno, pode mudar) → evento de INTEGRAÇÃO (público, estável).
    private static OrdemProcessadaMensagem ParaMensagem(IDomainEvent evento) => evento switch
    {
        OrdemAceita aceita => new(
            OrdemProcessadaMensagem.VersaoAtual, nameof(OrdemAceita),
            aceita.OrdemId, aceita.Ativo.ToString(), Sigla(aceita.Lado),
            aceita.Quantidade, aceita.Preco,
            Sucesso: true, ExposicaoAtual: aceita.ExposicaoResultante, MsgErro: null,
            aceita.OccurredOn),

        OrdemRejeitada rejeitada => new(
            OrdemProcessadaMensagem.VersaoAtual, nameof(OrdemRejeitada),
            rejeitada.OrdemId, rejeitada.Ativo.ToString(), Sigla(rejeitada.Lado),
            rejeitada.Quantidade, rejeitada.Preco,
            Sucesso: false, ExposicaoAtual: rejeitada.ExposicaoAtual,
            MsgErro: $"Erro aconteceu porque {rejeitada.Motivo}",
            rejeitada.OccurredOn),

        _ => throw new InvalidOperationException($"Evento sem mensagem de integração: {evento.GetType().Name}")
    };

    private static string Sigla(Lado lado) => lado == Lado.Compra ? "C" : "V";
}