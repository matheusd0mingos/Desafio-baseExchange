using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.Consultas;

namespace OrderAccumulator.Infrastructure.Persistence.Consultas;

public sealed class EfConsultasOrdens(OrderAccumulatorDbContext db) : IConsultasOrdens
{
    public async Task<IReadOnlyList<ExposicaoResumo>> ListarExposicoesAsync(CancellationToken ct = default) =>
        await db.Exposicoes.AsNoTracking()
            .OrderBy(e => e.Ativo)
            .Select(e => new ExposicaoResumo(e.Ativo, e.Valor))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OrdemAceitaResumo>> ListarOrdensAceitasAsync(int limite, CancellationToken ct = default) =>
        await db.OrdensAceitas.AsNoTracking()
            .OrderByDescending(o => o.OccurredOn)
            .Take(limite)
            .Select(o => new OrdemAceitaResumo(o.OrdemId, o.Ativo, o.Lado, o.Quantidade, o.Preco,
                o.ExposicaoResultante, o.OccurredOn))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MensagemOutboxResumo>> ListarOutboxAsync(int limite, CancellationToken ct = default) =>
        await db.Outbox.AsNoTracking()
            .OrderByDescending(m => m.CriadaEm)
            .Take(limite)
            .Select(m => new MensagemOutboxResumo(m.Id, m.Tipo, m.Chave, m.CriadaEm, m.PublicadaEm))
            .ToListAsync(ct);
}