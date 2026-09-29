using System.ComponentModel.DataAnnotations;
using OrderExposure.Contracts.Requests;

namespace OrderGenerator.Formularios;

/// <summary>O modelo do formulário. Valida antes de enviar, para dar resposta rápida ao usuário.</summary>
public sealed class OrdemFormulario
{
    [Required]
    public string Ativo { get; set; } = "PETR4";

    [Required]
    public string Lado { get; set; } = "C";

    [Range(1, 99_999, ErrorMessage = "A quantidade deve ser um inteiro entre 1 e 99.999.")]
    public int Quantidade { get; set; } = 100;

    [PrecoValido]
    public decimal Preco { get; set; } = 10.00m;

    public NovaOrdemRequest ParaRequest() => new(Ativo, Lado, Quantidade, Preco);
}