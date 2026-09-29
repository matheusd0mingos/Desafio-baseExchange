using OrderAccumulator.Application.UseCases.ProcessarOrdem;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.SharedKernel;
using OrderExposure.Contracts.Requests;
using OrderExposure.Contracts.Responses;

namespace OrderAccumulator.Api.Mapeamento;

/// <summary>A borda: traduz o idioma do JSON ("C", "PETR4") para o idioma do domínio, e de volta.</summary>
public static class OrdemMapeamento
{
    public static Result<ProcessarOrdemCommand> ParaCommand(NovaOrdemRequest request, Guid ordemId)
    {
        if (!Enum.TryParse<Ativo>(request.Ativo, ignoreCase: false, out var ativo) || !Enum.IsDefined(ativo))
            return Result.Failure<ProcessarOrdemCommand>(
                new Error("Requisicao.AtivoInvalido", "o ativo deve ser PETR4, VALE3 ou VIIA4."));

        Lado? lado = request.Lado switch
        {
            "C" => Lado.Compra,
            "V" => Lado.Venda,
            _ => null
        };

        if (lado is null)
            return Result.Failure<ProcessarOrdemCommand>(
                new Error("Requisicao.LadoInvalido", "o lado deve ser \"C\" (compra) ou \"V\" (venda)."));

        return Result.Success(new ProcessarOrdemCommand(ordemId, ativo, lado.Value, request.Quantidade, request.Preco));
    }

    public static OrdemResponse ParaResponse(ProcessarOrdemResult resultado) =>
        new(resultado.Sucesso, resultado.ExposicaoAtual, MensagemDe(resultado.Erro));

    public static string? MensagemDe(Error? erro) =>
        erro is null ? null : $"Erro aconteceu porque {erro.Message}";

    /// <summary>O código do erro decide o status HTTP. Por isso o Error tem código, não só texto.</summary>
    public static int StatusHttp(ProcessarOrdemResult resultado) => resultado.Erro?.Code switch
    {
        null => StatusCodes.Status200OK,
        var codigo when codigo.StartsWith("Exposicao.") => StatusCodes.Status422UnprocessableEntity,
        var codigo when codigo.StartsWith("Aplicacao.") => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status400BadRequest
    };
}