using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.Exposicoes;
using OrderAccumulator.Domain.Ordens;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class EfExposicaoRepository(OrderAccumulatorDbContext db) : IExposicaoRepository
{
    public async Task<ExposicaoAtivo> ObterAsync(Ativo ativo, CancellationToken ct = default)
    {
        var linha = await db.Exposicoes.AsNoTracking().FirstOrDefaultAsync(e => e.Ativo == ativo, ct);

        return linha is null
            ? ExposicaoAtivo.Criar(ativo)
            : ExposicaoAtivo.Reconstituir(ativo, linha.Valor);
    }

    public async Task<ExposicaoAtivo> ObterParaAtualizarAsync(Ativo ativo, CancellationToken ct = default)
    {
        var linhas = await db.Exposicoes
            .FromSql($"SELECT ativo, valor FROM exposicoes WHERE ativo = {ativo.ToString()} FOR UPDATE")
            .ToListAsync(ct);

        var linha = linhas.SingleOrDefault()
            ?? throw new InvalidOperationException($"A linha de {ativo} não existe. As migrations foram aplicadas?");

        // Sempre um objeto novo a partir da linha: mexer nele não altera o banco até o SalvarAsync.
        return ExposicaoAtivo.Reconstituir(ativo, linha.Valor);
    }

    public async Task SalvarAsync(ExposicaoAtivo exposicao, CancellationToken ct = default)
    {
        var linha = await db.Exposicoes.FindAsync([exposicao.Ativo], ct)
            ?? throw new InvalidOperationException($"A linha de {exposicao.Ativo} não existe.");

        linha.Valor = exposicao.Valor; // só marca a mudança; vai para o banco no ConfirmarAsync
    }
}