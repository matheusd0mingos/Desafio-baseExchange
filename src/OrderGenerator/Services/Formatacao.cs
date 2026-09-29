using System.Globalization;

namespace OrderGenerator.Services;

/// <summary>Formatação brasileira num lugar só, usada por todas as telas.</summary>
public static class Formatacao
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    public static string Percentual(decimal valor) => valor.ToString("0.00", PtBr) + "%";

    public static string Hora(DateTimeOffset momento) => momento.ToLocalTime().ToString("HH:mm:ss");

    public static string Lado(string lado) => lado == "C" ? "Compra" : "Venda";

    /// <summary>Para CSS: sempre com ponto decimal ("3.2"), nunca vírgula, senão o navegador ignora.</summary>
    public static string Css(decimal valor) => valor.ToString(CultureInfo.InvariantCulture);
}