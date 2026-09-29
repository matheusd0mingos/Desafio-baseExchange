using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderAccumulator.Infrastructure.Persistence;

/// <summary>
/// Usada só pela ferramenta "dotnet ef" para gerar migrations.
/// Gerar migration não conecta no banco; a connection string aqui é só um molde.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderAccumulatorDbContext>
{
    public OrderAccumulatorDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OrderAccumulatorDbContext>()
            .UseNpgsql("Host=localhost;Database=orderdb;Username=postgres;Password=postgres")
            .Options);
}