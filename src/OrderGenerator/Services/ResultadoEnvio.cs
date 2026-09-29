using OrderExposure.Contracts.Responses;

namespace OrderGenerator.Services;

public sealed record ResultadoEnvio(int StatusHttp, OrdemResponse Resposta)
{
    /// <summary>503 (banco fora) ou sem conexão: reenviar com a MESMA chave é seguro.</summary>
    public bool PodeTentarDeNovo => StatusHttp is 0 or 503;

    public static ResultadoEnvio SemConexao(string mensagem) =>
        new(0, new OrdemResponse(false, 0m, mensagem));
}