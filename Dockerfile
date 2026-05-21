FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Nido.Api/Nido.Api.csproj src/Nido.Api/
COPY src/Nido.Application/Nido.Application.csproj src/Nido.Application/
COPY src/Nido.Domain/Nido.Domain.csproj src/Nido.Domain/
COPY src/Nido.Infrastructure/Nido.Infrastructure.csproj src/Nido.Infrastructure/

RUN dotnet restore src/Nido.Api/Nido.Api.csproj

COPY src/ src/

RUN dotnet publish src/Nido.Api/Nido.Api.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Nido.Api.dll"]
