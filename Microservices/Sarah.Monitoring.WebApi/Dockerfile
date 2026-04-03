FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Microservices/Sarah.Monitoring.WebApi/Sarah.Monitoring.WebApi.csproj", "Microservices/Sarah.Monitoring.WebApi/"]
COPY ["Libs/Sarah.API/Sarah.API.csproj", "Libs/Sarah.API/"]
COPY ["Libs/Sarah.Authentication/Sarah.Authentication.csproj", "Libs/Sarah.Authentication/"]
COPY ["Libs/Sarah.Messaging.RabbitMQ/Sarah.Messaging.RabbitMQ.csproj", "Libs/Sarah.Messaging.RabbitMQ/"]
COPY ["Libs/Sarah.ServiceClients/Sarah.ServiceClients.csproj", "Libs/Sarah.ServiceClients/"]
COPY ["Libs/Sarah.ServiceDefaults/Sarah.ServiceDefaults.csproj", "Libs/Sarah.ServiceDefaults/"]
RUN dotnet restore "Microservices/Sarah.Monitoring.WebApi/Sarah.Monitoring.WebApi.csproj"
COPY . .
WORKDIR "/src/Microservices/Sarah.Monitoring.WebApi"
RUN dotnet build "Sarah.Monitoring.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sarah.Monitoring.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sarah.Monitoring.WebApi.dll"]
