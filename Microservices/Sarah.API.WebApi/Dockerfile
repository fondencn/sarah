FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Microservices/Sarah.API.WebApi/Sarah.API.WebApi.csproj", "Microservices/Sarah.API.WebApi/"]
COPY ["Services/Sarah.API/Sarah.API.csproj", "Services/Sarah.API/"]
COPY ["Services/Sarah.Logging/Sarah.Logging.csproj", "Services/Sarah.Logging/"]
RUN dotnet restore "Microservices/Sarah.API.WebApi/Sarah.API.WebApi.csproj"
COPY . .
WORKDIR "/src/Microservices/Sarah.API.WebApi"
RUN dotnet build "Sarah.API.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sarah.API.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sarah.API.WebApi.dll"]
