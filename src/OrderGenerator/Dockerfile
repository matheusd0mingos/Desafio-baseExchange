# Estágio 1: compilar o Blazor (imagem com SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ ./src/
RUN dotnet publish src/OrderGenerator -c Release -o /app

# Estágio 2: servir os arquivos estáticos (imagem nginx, leve)
FROM nginx:1.30-alpine
COPY src/OrderGenerator/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/wwwroot /usr/share/nginx/html
EXPOSE 80