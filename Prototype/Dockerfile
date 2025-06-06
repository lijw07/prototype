# Base Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Prototype/Prototype.csproj", "Prototype/"]
RUN dotnet restore
RUN dotnet restore "Prototype/Prototype.csproj"
COPY . .
WORKDIR "/src/Prototype"
RUN dotnet build "./Prototype.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Prototype.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Hot Reload
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /src/Prototype
COPY . .
RUN dotnet restore
EXPOSE 8080
EXPOSE 8081
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]

# Final Runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Prototype.dll"]
