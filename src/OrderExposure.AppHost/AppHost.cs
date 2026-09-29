var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()   // os dados sobrevivem a um restart do AppHost
    .WithPgAdmin();     // painel web para ver as tabelas

var orderDb = postgres.AddDatabase("orderdb");

builder.AddProject<Projects.OrderAccumulator_Api>("api")
    .WithReference(orderDb) // injeta ConnectionStrings:orderdb na API
    .WaitFor(orderDb);      // a API só sobe com o banco pronto

builder.Build().Run();