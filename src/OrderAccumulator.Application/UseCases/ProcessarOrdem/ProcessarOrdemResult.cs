using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Application.UseCases.ProcessarOrdem;

public sealed record ProcessarOrdemResult(bool Sucesso, decimal ExposicaoAtual, Error? Erro)
{
    public static ProcessarOrdemResult Aceita(decimal exposicaoAtual) =>
        new(true, exposicaoAtual, null);

    public static ProcessarOrdemResult Rejeitada(decimal exposicaoAtual, Error erro) =>
        new(false, exposicaoAtual, erro);
}