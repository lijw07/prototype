FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dev
WORKDIR /src

COPY ["Prototype.csproj", "./"]
RUN dotnet restore

COPY . .

# Optional debugger setup
RUN apt-get update && \
    apt-get install -y curl unzip && \
    mkdir -p /vsdbg && \
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

EXPOSE 8080
EXPOSE 57404

CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:8080"]