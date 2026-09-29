using Microsoft.Extensions.DependencyInjection;
using OrderAccumulator.Application.Concorrencia;
using OrderAccumulator.Application.Observers;
using OrderAccumulator.Application.UseCases.ProcessarOrdem;
using OrderAccumulator.Domain.Ordens.Strategies;

namespace OrderAccumulator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<ILadoStrategy, CompraStrategy>();
        services.AddSingleton<ILadoStrategy, VendaStrategy>();
        services.AddSingleton<ILadoStrategyFactory, LadoStrategyFactory>();

        // Singleton obrigatório: um só conjunto de filas para a aplicação inteira.
        services.AddSingleton<AtivoLocks>();

        services.AddScoped<OrderEventNotifier>();
        services.AddScoped<ProcessarOrdemUseCase>();

        return services;
    }
}