using System.Text.Json.Serialization;

namespace OrderExposure.Contracts.Responses;

/// <summary>O JSON de resposta, exatamente como o edital define.</summary>
public sealed record OrdemResponse(
    [property: JsonPropertyName("sucesso")] bool Sucesso,
    [property: JsonPropertyName("exposicao_atual")] decimal ExposicaoAtual,
    [property: JsonPropertyName("msg_erro")] string? MsgErro);