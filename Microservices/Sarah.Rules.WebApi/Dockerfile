FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Microservices/Sarah.Rules.WebApi/Sarah.Rules.WebApi.csproj", "Microservices/Sarah.Rules.WebApi/"]
COPY ["Services/Sarah.Rules/Sarah.Rules.csproj", "Services/Sarah.Rules/"]
COPY ["Services/Sarah.API/Sarah.API.csproj", "Services/Sarah.API/"]
COPY ["Services/Sarah.Logging/Sarah.Logging.csproj", "Services/Sarah.Logging/"]
RUN dotnet restore "Microservices/Sarah.Rules.WebApi/Sarah.Rules.WebApi.csproj"
COPY . .
WORKDIR "/src/Microservices/Sarah.Rules.WebApi"
RUN dotnet build "Sarah.Rules.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sarah.Rules.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sarah.Rules.WebApi.dll"]
