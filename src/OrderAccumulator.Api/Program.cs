using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application;
using OrderAccumulator.Infrastructure;
using OrderAccumulator.Infrastructure.Persistence;
using OrderExposure.Contracts.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
        // JSON malformado ou campo faltando também responde no formato do edital
        options.InvalidModelStateResponseFactory = _ => new BadRequestObjectResult(
            new OrdemResponse(false, 0m, "Erro aconteceu porque o JSON enviado é inválido ou está incompleto.")));
builder.Services.AddOpenApi();

var app = builder.Build();

// Cria/atualiza as tabelas ao subir (desafio: simples e reproduzível).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderAccumulatorDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapControllers();

app.Run();