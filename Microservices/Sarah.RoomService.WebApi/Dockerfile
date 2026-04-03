FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Microservices/Sarah.RoomService.WebApi/Sarah.RoomService.WebApi.csproj", "Microservices/Sarah.RoomService.WebApi/"]
COPY ["Libs/Sarah.Authentication/Sarah.Authentication.csproj", "Libs/Sarah.Authentication/"]
COPY ["Libs/Sarah.ServiceDefaults/Sarah.ServiceDefaults.csproj", "Libs/Sarah.ServiceDefaults/"]
RUN dotnet restore "Microservices/Sarah.RoomService.WebApi/Sarah.RoomService.WebApi.csproj"
COPY . .
WORKDIR "/src/Microservices/Sarah.RoomService.WebApi"
RUN dotnet build "Sarah.RoomService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sarah.RoomService.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sarah.RoomService.WebApi.dll"]
