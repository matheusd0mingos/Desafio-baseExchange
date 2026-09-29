using System.Net.Http.Json;
using System.Text.Json;
using OrderExposure.Contracts.Responses;

namespace OrderGenerator.Services;

/// <summary>Só leitura: busca exposições, ordens e caixa de saída. Devolve null se a API não responder.</summary>
public sealed class ConsultasApiClient(HttpClient http)
{
    public Task<List<ExposicaoResponse>?> ExposicoesAsync(CancellationToken ct = default) =>
        ObterAsync<List<ExposicaoResponse>>("api/exposicoes", ct);

    public Task<List<OrdemAceitaResponse>?> OrdensAsync(int limite = 50, CancellationToken ct = default) =>
        ObterAsync<List<OrdemAceitaResponse>>($"api/ordens?limite={limite}", ct);

    public Task<List<MensagemOutboxResponse>?> OutboxAsync(int limite = 50, CancellationToken ct = default) =>
        ObterAsync<List<MensagemOutboxResponse>>($"api/outbox?limite={limite}", ct);

    private async Task<T?> ObterAsync<T>(string url, CancellationToken ct) where T : class
    {
        try
        {
            return await http.GetFromJsonAsync<T>(url, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            return null;
        }
    }
}