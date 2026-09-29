using Microsoft.AspNetCore.Mvc;
using OrderAccumulator.Api.Mapeamento;
using OrderAccumulator.Application.UseCases.ProcessarOrdem;
using OrderExposure.Contracts.Requests;
using OrderExposure.Contracts.Responses;

namespace OrderAccumulator.Api.Controllers;

[ApiController]
[Route("api/ordens")]
public sealed class OrdensController(ProcessarOrdemUseCase useCase) : ControllerBase
{
    /// <param name="idempotencyKey">Opcional. Reenviar com a mesma chave não conta a ordem duas vezes.</param>
    [HttpPost]
    [ProducesResponseType<OrdemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<OrdemResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<OrdemResponse>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<OrdemResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<OrdemResponse>> Criar(
        NovaOrdemRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
        CancellationToken ct)
    {
        var command = OrdemMapeamento.ParaCommand(request, idempotencyKey ?? Guid.NewGuid());
        if (command.IsFailure)
            return BadRequest(new OrdemResponse(false, 0m, OrdemMapeamento.MensagemDe(command.Error)));

        var resultado = await useCase.ExecutarAsync(command.Value, ct);

        return StatusCode(OrdemMapeamento.StatusHttp(resultado), OrdemMapeamento.ParaResponse(resultado));
    }
}