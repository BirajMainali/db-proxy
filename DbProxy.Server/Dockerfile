FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DbProxy.Server/DbProxy.Server.csproj", "DbProxy.Server/"]
RUN dotnet restore "DbProxy.Server/DbProxy.Server.csproj"
COPY . .
WORKDIR "/src/DbProxy.Server"
RUN dotnet build "./DbProxy.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DbProxy.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DbProxy.Server.dll"]
