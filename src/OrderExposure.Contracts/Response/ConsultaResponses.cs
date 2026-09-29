using System.Text.Json.Serialization;

namespace OrderExposure.Contracts.Responses;

public sealed record ExposicaoResponse(
    [property: JsonPropertyName("ativo")] string Ativo,
    [property: JsonPropertyName("exposicao_atual")] decimal ExposicaoAtual,
    [property: JsonPropertyName("limite")] decimal Limite,
    [property: JsonPropertyName("percentual_do_limite")] decimal PercentualDoLimite);

public sealed record OrdemAceitaResponse(
    [property: JsonPropertyName("ordem_id")] Guid OrdemId,
    [property: JsonPropertyName("ativo")] string Ativo,
    [property: JsonPropertyName("lado")] string Lado,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco,
    [property: JsonPropertyName("exposicao_resultante")] decimal ExposicaoResultante,
    [property: JsonPropertyName("ocorrida_em")] DateTimeOffset OcorridaEm);

public sealed record MensagemOutboxResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("tipo")] string Tipo,
    [property: JsonPropertyName("ativo")] string Ativo,
    [property: JsonPropertyName("criada_em")] DateTimeOffset CriadaEm,
    [property: JsonPropertyName("publicada_em")] DateTimeOffset? PublicadaEm,
    [property: JsonPropertyName("status")] string Status);