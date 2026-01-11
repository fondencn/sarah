FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Microservices/Sarah.Persons.WebApi/Sarah.Persons.WebApi.csproj", "Microservices/Sarah.Persons.WebApi/"]
COPY ["Services/Sarah.Persons/Sarah.Persons.csproj", "Services/Sarah.Persons/"]
COPY ["Services/Sarah.API/Sarah.API.csproj", "Services/Sarah.API/"]
COPY ["Services/Sarah.Logging/Sarah.Logging.csproj", "Services/Sarah.Logging/"]
RUN dotnet restore "Microservices/Sarah.Persons.WebApi/Sarah.Persons.WebApi.csproj"
COPY . .
WORKDIR "/src/Microservices/Sarah.Persons.WebApi"
RUN dotnet build "Sarah.Persons.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Sarah.Persons.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sarah.Persons.WebApi.dll"]
