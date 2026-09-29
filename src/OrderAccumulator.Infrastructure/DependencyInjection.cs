using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Infrastructure.Observers;
using OrderAccumulator.Infrastructure.Persistence;

namespace OrderAccumulator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("orderdb")
            ?? throw new InvalidOperationException("Connection string 'orderdb' não configurada.");

        services.AddDbContext<OrderAccumulatorDbContext>(options => options.UseNpgsql(connectionString));

        // Scoped: na mesma requisição, todos compartilham o MESMO DbContext, logo a MESMA transação.
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IExposicaoRepository, EfExposicaoRepository>();
        services.AddScoped<IOrdensAceitas, EfOrdensAceitas>();
        services.AddScoped<IOutbox, EfOutbox>();

        services.AddSingleton<IOrderEventObserver, LogOrderObserver>();

        return services;
    }
}