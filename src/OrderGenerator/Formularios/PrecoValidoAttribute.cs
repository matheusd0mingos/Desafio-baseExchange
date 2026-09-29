using System.ComponentModel.DataAnnotations;

namespace OrderGenerator.Formularios;

/// <summary>Mesma regra do Value Object Preco do domínio: positivo, menor que 1.000 e múltiplo de 0,01.</summary>
public sealed class PrecoValidoAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context) => value switch
    {
        decimal preco when preco <= 0 => new("O preço deve ser maior que zero."),
        decimal preco when preco >= 1_000 => new("O preço deve ser menor que R$ 1.000,00."),
        decimal preco when decimal.Round(preco, 2) != preco => new("O preço deve ser múltiplo de R$ 0,01."),
        decimal => ValidationResult.Success,
        _ => new("Informe um preço válido.")
    };
}