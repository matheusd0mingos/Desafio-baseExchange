using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Ordens;
using OrderAccumulator.Infrastructure.Persistence.Tabelas;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class OrderAccumulatorDbContext(DbContextOptions<OrderAccumulatorDbContext> options)
    : DbContext(options)
{
    public DbSet<ExposicaoRow> Exposicoes => Set<ExposicaoRow>();
    public DbSet<OrdemAceitaRow> OrdensAceitas => Set<OrdemAceitaRow>();
    public DbSet<OutboxMensagem> Outbox => Set<OutboxMensagem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExposicaoRow>(tabela =>
        {
            tabela.ToTable("exposicoes");
            tabela.HasKey(e => e.Ativo);
            tabela.Property(e => e.Ativo).HasColumnName("ativo").HasConversion<string>().HasMaxLength(10);
            tabela.Property(e => e.Valor).HasColumnName("valor").HasPrecision(18, 2);

            // Uma linha por ativo desde a criação do banco: o FOR UPDATE sempre tem o que travar.
            tabela.HasData(Enum.GetValues<Ativo>().Select(ativo => new ExposicaoRow { Ativo = ativo, Valor = 0m }));
        });

        modelBuilder.Entity<OrdemAceitaRow>(tabela =>
        {
            tabela.ToTable("ordens_aceitas");
            tabela.HasKey(o => o.OrdemId);
            tabela.Property(o => o.OrdemId).HasColumnName("ordem_id");
            tabela.Property(o => o.Ativo).HasColumnName("ativo").HasConversion<string>().HasMaxLength(10);
            tabela.Property(o => o.Lado).HasColumnName("lado").HasConversion<string>().HasMaxLength(10);
            tabela.Property(o => o.Quantidade).HasColumnName("quantidade");
            tabela.Property(o => o.Preco).HasColumnName("preco").HasPrecision(10, 2);
            tabela.Property(o => o.ExposicaoResultante).HasColumnName("exposicao_resultante").HasPrecision(18, 2);
            tabela.Property(o => o.OccurredOn).HasColumnName("occurred_on");
        });

        modelBuilder.Entity<OutboxMensagem>(tabela =>
        {
            tabela.ToTable("outbox");
            tabela.HasKey(m => m.Id);
            tabela.Property(m => m.Id).HasColumnName("id");
            tabela.Property(m => m.Tipo).HasColumnName("tipo").HasMaxLength(100);
            tabela.Property(m => m.Chave).HasColumnName("chave").HasMaxLength(50);
            tabela.Property(m => m.Conteudo).HasColumnName("conteudo").HasColumnType("jsonb");
            tabela.Property(m => m.CriadaEm).HasColumnName("criada_em");
            tabela.Property(m => m.PublicadaEm).HasColumnName("publicada_em");

            // O publicador procura só as pendentes; o índice parcial deixa isso rápido.
            tabela.HasIndex(m => m.CriadaEm).HasFilter("publicada_em IS NULL");
        });
    }
}