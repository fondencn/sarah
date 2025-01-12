# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the container
WORKDIR /src


# Copy the rest of the application code
COPY . .

# Set the working directory to the project directory
WORKDIR /src/Sarah.LocationServer

# Build the application
RUN dotnet publish -c Release -o /app/out

# Use the official ASP.NET Core runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory inside the container
WORKDIR /app

# Copy the built application from the build stage
COPY --from=build /app/out .

# Set environment variables for the certificate file and password
ENV SARAH_PORT=5002
ENV SARAH_CERTIFICATE_FILE=/app/certificate.pfx
ENV SARAH_CERTIFICATE_PASSWORD=crypticpassword

# Expose the port the application runs on
EXPOSE 5002

# Run the application
ENTRYPOINT ["dotnet", "Sarah.LocationServer.dll"]