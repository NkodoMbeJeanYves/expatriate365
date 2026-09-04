# Dockerfile — expatriate365 API
# Target framework: net10.0
# Build: multi-stage (build → runtime)
# Used by docker-compose.yml; can also be run standalone.

# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (layer cache)
COPY server.csproj ./
RUN dotnet restore server.csproj

# Copy source and publish
COPY . ./
RUN dotnet publish server.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# ── Stage 2: runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create upload directories
RUN mkdir -p downloads/attachments downloads/avatars downloads/branding downloads/docs

# Copy published output
COPY --from=build /app/publish ./

# Port exposed by ASPNETCORE_URLS
EXPOSE 5001

# Runtime environment (overridden by docker-compose / env file)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:5001

ENTRYPOINT ["dotnet", "server.dll"]
