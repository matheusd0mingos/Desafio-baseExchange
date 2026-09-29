# Estágio 1: compilar (imagem com SDK, pesada)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ ./src/
RUN dotnet publish src/OrderAccumulator.Api -c Release -o /app

# Estágio 2: rodar (imagem só com runtime, leve)
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderAccumulator.Api.dll"]