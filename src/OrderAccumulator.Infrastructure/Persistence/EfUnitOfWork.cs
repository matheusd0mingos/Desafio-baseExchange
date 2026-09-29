using Microsoft.EntityFrameworkCore.Storage;
using OrderAccumulator.Application.Abstractions;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class EfUnitOfWork(OrderAccumulatorDbContext db) : IUnitOfWork
{
    public async Task<ITransacao> IniciarTransacaoAsync(CancellationToken ct = default) =>
        new EfTransacao(db, await db.Database.BeginTransactionAsync(ct));

    private sealed class EfTransacao(OrderAccumulatorDbContext db, IDbContextTransaction transacao) : ITransacao
    {
        private bool _confirmada;

        public async Task ConfirmarAsync(CancellationToken ct = default)
        {
            await db.SaveChangesAsync(ct);   // manda as gravações pendentes para o banco
            await transacao.CommitAsync(ct); // e assina tudo de uma vez
            _confirmada = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_confirmada)
                db.ChangeTracker.Clear();    // esquece o rascunho que não foi assinado

            await transacao.DisposeAsync();  // sem Commit, o Postgres desfaz tudo e solta as travas
        }
    }
}