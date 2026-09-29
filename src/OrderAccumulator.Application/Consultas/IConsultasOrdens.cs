namespace OrderAccumulator.Application.Consultas;

/// <summary>
/// O lado de LEITURA (o "Q" do CQRS): consultas simples, sem regra de negócio
/// e sem passar pelo aggregate. Nunca altera nada.
/// </summary>
public interface IConsultasOrdens
{
    Task<IReadOnlyList<ExposicaoResumo>> ListarExposicoesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<OrdemAceitaResumo>> ListarOrdensAceitasAsync(int limite, CancellationToken ct = default);

    Task<IReadOnlyList<MensagemOutboxResumo>> ListarOutboxAsync(int limite, CancellationToken ct = default);
}