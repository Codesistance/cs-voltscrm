# syntax=docker/dockerfile:1
# Build context MUST be the repo root: docker build -f src/backend/worker/VoltsCRM.Worker/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Directory.Build.props enables UseArtifactsOutput, which relocates obj/ to
# src/backend/artifacts/. It MUST be present during restore so `publish --no-restore`
# below looks for the assets file in the same place restore wrote it.
COPY src/backend/Directory.Build.props src/backend/

# Copy csproj files first for restore-layer caching.
COPY src/backend/worker/VoltsCRM.Worker/VoltsCRM.Worker.csproj src/backend/worker/VoltsCRM.Worker/
COPY src/backend/libraries/VoltsCRM.Application/VoltsCRM.Application.csproj src/backend/libraries/VoltsCRM.Application/
COPY src/backend/libraries/VoltsCRM.Domain/VoltsCRM.Domain.csproj src/backend/libraries/VoltsCRM.Domain/
RUN dotnet restore src/backend/worker/VoltsCRM.Worker/VoltsCRM.Worker.csproj

# Copy the rest of the backend source and publish.
COPY src/backend/ src/backend/
RUN dotnet publish src/backend/worker/VoltsCRM.Worker/VoltsCRM.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app ./

ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "VoltsCRM.Worker.dll"]
