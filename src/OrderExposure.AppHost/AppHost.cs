using Aspire.Hosting.Yarp;

var builder = DistributedApplication.CreateBuilder(args);

// Banco: a fonte da verdade
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var orderDb = postgres.AddDatabase("orderdb");

// Kafka: a distribuição das notas
var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

// OrderAccumulator: espera o banco, NÃO espera o Kafka
var api = builder.AddProject<Projects.OrderAccumulator_Api>("api")
    .WithReference(orderDb)
    .WaitFor(orderDb)
    .WithReference(kafka);

// OrderGenerator: o Blazor
var front = builder.AddProject<Projects.OrderGenerator>("front");

// Portão de entrada: faz no dev o papel que o nginx faz no Compose.
// "/api/..." vai para a API; o resto vai para o Blazor. Mesma origem, sem CORS.
builder.AddYarp("gateway")
    .WithHostPort(5100)
    .WithConfiguration(yarp =>
    {
        // Ordem explícita: /api é avaliada ANTES do "pega-tudo" do front (menor número = antes).
        yarp.AddRoute("/api/{**catch-all}", api).WithOrder(1);
        yarp.AddRoute("{**catch-all}", front).WithOrder(100);
    })
    .WaitFor(api)
    .WaitFor(front);

builder.Build().Run();