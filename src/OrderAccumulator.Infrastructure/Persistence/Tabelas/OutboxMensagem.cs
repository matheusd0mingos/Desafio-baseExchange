namespace OrderAccumulator.Infrastructure.Persistence.Tabelas;

/// <summary>Uma linha da tabela "outbox": um evento esperando para ser publicado no Kafka.</summary>
public sealed class OutboxMensagem
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public string Conteudo { get; set; } = string.Empty;
    public DateTimeOffset CriadaEm { get; set; }
    public DateTimeOffset? PublicadaEm { get; set; }
}