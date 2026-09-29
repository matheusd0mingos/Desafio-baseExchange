using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OrderGenerator;
using OrderGenerator.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Mesma origem: o front chama "/api/..." no próprio endereço; quem repassa para a API é o proxy (nginx).
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<OrdemApiClient>();
builder.Services.AddScoped<ConsultasApiClient>();

await builder.Build().RunAsync();