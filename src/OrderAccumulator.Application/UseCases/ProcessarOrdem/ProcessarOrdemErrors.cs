using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Application.UseCases.ProcessarOrdem;

public static class ProcessarOrdemErrors
{
    public static readonly Error PersistenciaIndisponivel =
        new("Aplicacao.PersistenciaIndisponivel",
            "não foi possível registrar a ordem com segurança; tente novamente.");

    public static readonly Error ChaveReutilizada =
        new("Requisicao.ChaveReutilizada",
            "a Idempotency-Key informada já foi usada para uma ordem diferente.");
}