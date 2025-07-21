FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY HealthCheckApi/*.csproj ./HealthCheckApi/
RUN dotnet restore ./HealthCheckApi/HealthCheckApi.csproj


COPY . .
RUN dotnet publish ./HealthCheckApi/HealthCheckApi.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HealthCheckApi.dll"]