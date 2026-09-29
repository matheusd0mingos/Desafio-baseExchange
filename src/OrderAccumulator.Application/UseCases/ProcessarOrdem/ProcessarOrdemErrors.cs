using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Application.UseCases.ProcessarOrdem;

public static class ProcessarOrdemErrors
{
    public static readonly Error LogIndisponivel =
        new("Aplicacao.LogIndisponivel",
            "não foi possível registrar a ordem com segurança; tente novamente.");
}