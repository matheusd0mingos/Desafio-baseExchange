using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<OrderEventNotifier>();
        services.AddScoped<ProcessarOrdemUseCase>();

        return services;
    }
}