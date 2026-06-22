# syntax=docker/dockerfile:1
# Build context MUST be the repo root: docker build -f src/backend/api/VoltsCRM.API/Dockerfile .
# (project references span src/backend/**, so the whole backend tree must be in context.)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Directory.Build.props enables UseArtifactsOutput, which relocates obj/ to
# src/backend/artifacts/. It MUST be present during restore so `publish --no-restore`
# below looks for the assets file in the same place restore wrote it.
COPY src/backend/Directory.Build.props src/backend/

# Copy csproj files first for restore-layer caching.
COPY src/backend/api/VoltsCRM.API/VoltsCRM.API.csproj src/backend/api/VoltsCRM.API/
COPY src/backend/libraries/VoltsCRM.Application/VoltsCRM.Application.csproj src/backend/libraries/VoltsCRM.Application/
COPY src/backend/libraries/VoltsCRM.Domain/VoltsCRM.Domain.csproj src/backend/libraries/VoltsCRM.Domain/
COPY src/backend/libraries/VoltsCRM.Infrastructure/VoltsCRM.Infrastructure.csproj src/backend/libraries/VoltsCRM.Infrastructure/
RUN dotnet restore src/backend/api/VoltsCRM.API/VoltsCRM.API.csproj

# Copy the rest of the backend source and publish.
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/api/VoltsCRM.API/VoltsCRM.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl is required by the ECS container health check (curl -f http://localhost:8080/health).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "VoltsCRM.API.dll"]
