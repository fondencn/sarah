FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5007

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Microservices/Sarah.Dashboard.WebApi/Sarah.Dashboard.WebApi.csproj", "Microservices/Sarah.Dashboard.WebApi/"]
COPY ["Libs/Sarah.API/Sarah.API.csproj", "Libs/Sarah.API/"]
COPY ["Libs/Sarah.Authentication/Sarah.Authentication.csproj", "Libs/Sarah.Authentication/"]
RUN dotnet restore "Microservices/Sarah.Dashboard.WebApi/Sarah.Dashboard.WebApi.csproj"
COPY . .
WORKDIR "/src/Microservices/Sarah.Dashboard.WebApi"
RUN dotnet build "Sarah.Dashboard.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sarah.Dashboard.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sarah.Dashboard.WebApi.dll"]
