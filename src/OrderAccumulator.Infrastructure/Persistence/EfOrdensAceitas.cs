using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Domain.Exposicoes.Events;
using OrderAccumulator.Infrastructure.Persistence.Tabelas;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class EfOrdensAceitas(OrderAccumulatorDbContext db) : IOrdensAceitas
{
    public async Task<OrdemAceita?> ObterAsync(Guid ordemId, CancellationToken ct = default)
    {
        var linha = await db.OrdensAceitas.AsNoTracking().FirstOrDefaultAsync(o => o.OrdemId == ordemId, ct);

        return linha is null
            ? null
            : new OrdemAceita(linha.OrdemId, linha.Ativo, linha.Lado, linha.Quantidade,
                linha.Preco, linha.ExposicaoResultante, linha.OccurredOn);
    }

    public Task RegistrarAsync(OrdemAceita evento, CancellationToken ct = default)
    {
        db.OrdensAceitas.Add(new OrdemAceitaRow
        {
            OrdemId = evento.OrdemId,
            Ativo = evento.Ativo,
            Lado = evento.Lado,
            Quantidade = evento.Quantidade,
            Preco = evento.Preco,
            ExposicaoResultante = evento.ExposicaoResultante,
            OccurredOn = evento.OccurredOn
        });

        return Task.CompletedTask;
    }
}