FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Nido.Api/Nido.Api.csproj src/Nido.Api/
COPY src/Nido.Application/Nido.Application.csproj src/Nido.Application/
COPY src/Nido.Domain/Nido.Domain.csproj src/Nido.Domain/
COPY src/Nido.Infrastructure/Nido.Infrastructure.csproj src/Nido.Infrastructure/
COPY src/Nido.Migrator/Nido.Migrator.csproj src/Nido.Migrator/

RUN dotnet restore src/Nido.Api/Nido.Api.csproj \
 && dotnet restore src/Nido.Migrator/Nido.Migrator.csproj

COPY src/ src/

RUN dotnet publish src/Nido.Api/Nido.Api.csproj \
    -c Release \
    -o /app/api

RUN dotnet publish src/Nido.Migrator/Nido.Migrator.csproj \
    -c Release \
    -o /app/migrator

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=build /app/migrator .
ENTRYPOINT ["dotnet", "Nido.Migrator.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/api .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Nido.Api.dll"]
