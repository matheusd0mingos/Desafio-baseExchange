using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderAccumulator.Application.Abstractions;
using OrderAccumulator.Infrastructure.Observers;
using OrderAccumulator.Infrastructure.Persistence;
using OrderAccumulator.Application.Consultas;
using OrderAccumulator.Infrastructure.Persistence.Consultas;
using Confluent.Kafka;
using OrderAccumulator.Infrastructure.Messaging;


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
        services.AddScoped<IConsultasOrdens, EfConsultasOrdens>();   

        services.AddSingleton<IOrderEventObserver, LogOrderObserver>();

        AddMensageiro(services, configuration);

        return services;
    }

    private static void AddMensageiro(IServiceCollection services, IConfiguration configuration)
    {
        var kafka = configuration.GetConnectionString("kafka");

        // Sem Kafka configurado, a API funciona normalmente: as mensagens só ficam esperando na outbox.
        if (string.IsNullOrWhiteSpace(kafka))
            return;

        services.AddSingleton<IProducer<string, string>>(_ => new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = kafka,
            Acks = Acks.All,          // só confirma depois de gravado com segurança
            EnableIdempotence = true, // retries internos não duplicam mensagem
            MessageTimeoutMs = 10_000 // Kafka fora: desiste em 10 s e tenta no próximo ciclo
        }).Build());

        services.AddHostedService<OutboxPublisher>();
    }

}