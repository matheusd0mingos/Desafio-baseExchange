var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()   // os dados sobrevivem a um restart do AppHost
    .WithPgAdmin();     // painel web para ver as tabelas

var orderDb = postgres.AddDatabase("orderdb");

var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();     // painel web para ver os tópicos e mensagens

builder.AddProject<Projects.OrderAccumulator_Api>("api")
    .WithReference(orderDb) // injeta ConnectionStrings:orderdb
    .WaitFor(orderDb)       // sem banco não dá para aceitar ordens: espera
    .WithReference(kafka);  // injeta ConnectionStrings:kafka
                            // SEM WaitFor(kafka): a API aceita ordens mesmo com o Kafka fora

builder.Build().Run();