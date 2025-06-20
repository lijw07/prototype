# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["Prototype.csproj", "./"]
RUN dotnet restore

# Copy source code
COPY . .

# Build and publish
RUN dotnet build "Prototype.csproj" -c Release -o /app/build
RUN dotnet publish "Prototype.csproj" -c Release -o /app/publish --no-restore

# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /src

# Copy csproj and restore as distinct layers for better caching
COPY ["Prototype.csproj", "./"]
RUN dotnet restore

# Copy everything else
COPY . .

# Install debugging tools and other utilities
RUN apt-get update && \
    apt-get install -y curl unzip procps htop && \
    mkdir -p /vsdbg && \
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg && \
    rm -rf /var/lib/apt/lists/*

# Enable file watcher for hot reload
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV DOTNET_ENVIRONMENT=Development

# Create non-root user for security
RUN adduser --disabled-password --gecos '' dotnetuser && \
    chown -R dotnetuser:dotnetuser /src

USER dotnetuser

EXPOSE 8080
EXPOSE 57404

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]

# Production stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS prod
WORKDIR /app

# Install curl for health checks
RUN apt-get update && \
    apt-get install -y curl && \
    rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN adduser --disabled-password --gecos '' dotnetuser

# Copy from build stage
COPY --from=build /app/publish .

# Set ownership
RUN chown -R dotnetuser:dotnetuser /app
USER dotnetuser

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Prototype.dll"]