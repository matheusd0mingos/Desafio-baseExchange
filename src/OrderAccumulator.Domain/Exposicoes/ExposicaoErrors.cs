using System.Globalization;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Domain.SharedKernel;

namespace OrderAccumulator.Domain.Exposicoes;

public static class ExposicaoErrors
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static Error LimiteExcedido(Ativo ativo, decimal exposicaoResultante) =>
        new("Exposicao.LimiteExcedido",
            $"a ordem levaria a exposição de {ativo} a {exposicaoResultante.ToString("C", PtBr)}, " +
            $"ultrapassando o limite de {ExposicaoAtivo.Limite.ToString("C", PtBr)}.");
}