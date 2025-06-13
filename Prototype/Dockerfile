# --- Base Runtime Image (for final production) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 57404

# --- Build Stage (for publish/production) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ./Prototype/*.csproj ./Prototype/
RUN dotnet restore "./Prototype/Prototype.csproj"
COPY . .
WORKDIR /src/Prototype
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

# --- Publish Stage (for publish/production) ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Development (hot reload, debugging, live code mount) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /src/Prototype

RUN apt-get update && \
    apt-get install -y curl unzip && \
    mkdir -p /vsdbg && \
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

EXPOSE 8080
EXPOSE 57404
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

CMD ["sh", "-c", "dotnet restore && dotnet watch run --urls http://0.0.0.0:8080"]

# --- Final Production Runtime ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Prototype.dll"]