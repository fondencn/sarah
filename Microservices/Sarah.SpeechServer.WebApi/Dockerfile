FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
# ALSA: aplay (used by SpeechService.PlaySound) + libasound2 (Azure Speech SDK microphone input)
RUN apt-get update && apt-get install -y --no-install-recommends \
    alsa-utils \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Microservices/Sarah.SpeechServer.WebApi/Sarah.SpeechServer.WebApi.csproj", "Microservices/Sarah.SpeechServer.WebApi/"]
COPY ["Libs/Sarah.API/Sarah.API.csproj", "Libs/Sarah.API/"]
COPY ["Libs/Sarah.ServiceClients/Sarah.ServiceClients.csproj", "Libs/Sarah.ServiceClients/"]
COPY ["Libs/Sarah.LEDService/Sarah.LEDService.csproj", "Libs/Sarah.LEDService/"]
COPY ["Libs/Sarah.Voice/Sarah.Voice.csproj", "Libs/Sarah.Voice/"]
COPY ["Libs/Sarah.Messaging.RabbitMQ/Sarah.Messaging.RabbitMQ.csproj", "Libs/Sarah.Messaging.RabbitMQ/"]
COPY ["Libs/Sarah.ServiceDefaults/Sarah.ServiceDefaults.csproj", "Libs/Sarah.ServiceDefaults/"]
RUN dotnet restore "Microservices/Sarah.SpeechServer.WebApi/Sarah.SpeechServer.WebApi.csproj"
COPY . .
WORKDIR "/src/Microservices/Sarah.SpeechServer.WebApi"
RUN dotnet build "Sarah.SpeechServer.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sarah.SpeechServer.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sarah.SpeechServer.WebApi.dll"]
