FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Microservices/Sarah.EventProcessing.WebApi/Sarah.EventProcessing.WebApi.csproj", "Microservices/Sarah.EventProcessing.WebApi/"]
COPY ["Services/Sarah.EventProcessing/Sarah.EventProcessing.csproj", "Services/Sarah.EventProcessing/"]
COPY ["Services/Sarah.API/Sarah.API.csproj", "Services/Sarah.API/"]
COPY ["Services/Sarah.Logging/Sarah.Logging.csproj", "Services/Sarah.Logging/"]
RUN dotnet restore "Microservices/Sarah.EventProcessing.WebApi/Sarah.EventProcessing.WebApi.csproj"
COPY . .
WORKDIR "/src/Microservices/Sarah.EventProcessing.WebApi"
RUN dotnet build "Sarah.EventProcessing.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sarah.EventProcessing.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sarah.EventProcessing.WebApi.dll"]
