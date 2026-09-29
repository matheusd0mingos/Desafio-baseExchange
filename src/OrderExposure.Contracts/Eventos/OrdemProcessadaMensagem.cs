using System.Text.Json.Serialization;

namespace OrderExposure.Contracts.Eventos;

/// <summary>
/// Evento de INTEGRAÇÃO publicado no Kafka: contrato público e estável,
/// no mesmo vocabulário do edital (o que entrou + o que foi respondido).
/// </summary>
public sealed record OrdemProcessadaMensagem(
    [property: JsonPropertyName("versao")] int Versao,
    [property: JsonPropertyName("evento")] string Evento,
    [property: JsonPropertyName("ordem_id")] Guid OrdemId,
    [property: JsonPropertyName("ativo")] string Ativo,
    [property: JsonPropertyName("lado")] string Lado,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco,
    [property: JsonPropertyName("sucesso")] bool Sucesso,
    [property: JsonPropertyName("exposicao_atual")] decimal ExposicaoAtual,
    [property: JsonPropertyName("msg_erro")] string? MsgErro,
    [property: JsonPropertyName("ocorrida_em")] DateTimeOffset OcorridaEm)
{
    public const int VersaoAtual = 1;
}