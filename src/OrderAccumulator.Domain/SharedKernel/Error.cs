namespace OrderAccumulator.Domain.SharedKernel;

/// <summary>Erro de negócio esperado (não é exceção): tem código estável e mensagem legível.</summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}