# Build stage
# Note: For Azure Foundry hosted agents, use --platform linux/amd64 when building on ARM (e.g., Apple Silicon)
#   docker build --platform linux/amd64 -t sales-to-signature .
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (layer caching)
COPY SalesToSignature.Agents.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Install wget for health checks (aspnet image includes neither curl nor wget)
RUN apt-get update && apt-get install -y --no-install-recommends wget && rm -rf /var/lib/apt/lists/*

LABEL org.opencontainers.image.source="https://github.com/greglevenhagen/multi-agent-workflows-az-funcs" \
      org.opencontainers.image.description="Sales-to-Signature Multi-Agent Pipeline" \
      org.opencontainers.image.title="sales-to-signature"

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DATA_DIRECTORY=/app/data

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/healthz || exit 1

COPY --from=build /app/publish .

# Mount data directory at runtime: docker run -v $(pwd)/data:/app/data ...
# The DATA_DIRECTORY env var tells tools where to find templates, rate cards, and legal files
ENTRYPOINT ["dotnet", "SalesToSignature.Agents.dll"]
