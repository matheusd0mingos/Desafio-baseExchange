using System.Net.Http.Json;
using System.Text.Json;
using OrderExposure.Contracts.Requests;
using OrderExposure.Contracts.Responses;

namespace OrderGenerator.Services;

public sealed class OrdemApiClient(HttpClient http)
{
    public async Task<ResultadoEnvio> EnviarAsync(NovaOrdemRequest request, Guid chaveIdempotencia, CancellationToken ct = default)
    {
        using var mensagem = new HttpRequestMessage(HttpMethod.Post, "api/ordens")
        {
            Content = JsonContent.Create(request)
        };
        mensagem.Headers.Add("Idempotency-Key", chaveIdempotencia.ToString());

        try
        {
            using var resposta = await http.SendAsync(mensagem, ct);
            var corpo = await resposta.Content.ReadFromJsonAsync<OrdemResponse>(ct);

            return corpo is null
                ? ResultadoEnvio.SemConexao("O servidor respondeu sem conteúdo.")
                : new ResultadoEnvio((int)resposta.StatusCode, corpo);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            return ResultadoEnvio.SemConexao("Não foi possível falar com o servidor. Tente novamente.");
        }
    }
}