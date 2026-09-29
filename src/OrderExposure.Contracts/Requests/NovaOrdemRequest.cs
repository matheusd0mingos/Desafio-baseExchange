using System.Text.Json.Serialization;

namespace OrderExposure.Contracts.Requests;

/// <summary>O JSON de entrada, exatamente como o edital define.</summary>
public sealed record NovaOrdemRequest(
    [property: JsonPropertyName("ativo")] string Ativo,
    [property: JsonPropertyName("lado")] string Lado,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco);