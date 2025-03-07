# Sarah

Chris's Smart Home

## Overview

Sarah is a smart home management system designed to provide users with a seamless and intuitive way to control and monitor their smart home devices. The system includes features such as device management, user-specific dashboards, and extended property handling for various smart devices.

## Technologies Used

- **ASP.NET Core**: For building the web API.
- **Entity Framework Core**: For database interactions.
- **SQLite**: As the database management system.
- **Angular**: For the front-end application.
- **OAuth2**: For authentication and authorization.
- **Docker**: For containerization.
- **RabbitMQ**: For messaging and event processing.

## Project Structure

The project is organized into several key directories and files:

- **Sarah.Server**: The ASP.NET Core web API.
- **Sarah.Data**: The Entity Framework Core data access layer.
- **sarah.client**: The Angular front-end application.
- **Sarah.EventProcessing**: The event processing service using RabbitMQ.
- **Sarah.DeviceService**: The device management service.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (for the Angular front-end)
- [Docker](https://www.docker.com/) (for containerization)
- [RabbitMQ](https://www.rabbitmq.com/) (for messaging)

### Installation

1. Clone the repository:
    ```bash
    git clone https://github.com/yourusername/sarah.git
    cd sarah
    ```

2. Set up the database:
    ```bash
    cd Sarah.Data
    dotnet ef database update
    ```

3. Build and run the API:
    ```bash
    cd ../Sarah.Server
    dotnet run
    ```

4. Build and run the front-end application:
    ```bash
    cd ../sarah.client
    npm install
    ng serve
    ```

5. Open your browser and navigate to `https://localhost:4200` to access the front-end application.

### Running with Docker

1. Build and run the containers:
    ```bash
    docker-compose up --build
    ```

2. Open your browser and navigate to `https://localhost:4200` to access the front-end application.

## Usage

- **Dashboard**: View and manage your favorite smart home devices.
- **Device Management**: Add, edit, and remove devices from your smart home.
- **User Authentication**: Secure login and registration using OAuth2 and OIDC.

## Authentication and Authorization
 
Sarah uses OAuth2 for authentication and authorization. OpenID Connect (OIDC) is used for user login, providing an additional layer of identity verification on top of OAuth2. This ensures secure and reliable user authentication.

### Keycloak as an OIDC Provider

Keycloak is a popular open-source identity and access management solution that can be used as an OIDC provider for Sarah. It provides features such as single sign-on (SSO), user federation, and identity brokering. You can configure Sarah to use Keycloak for OIDC by setting up the appropriate client and realm configurations in Keycloak and updating the OAuth2 settings in Sarah.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any changes.

## License

This project is licensed under the MIT License. See the [LICENSE](http://_vscodecontentref_/1) file for details.

For any questions or inquiries, please contact Chris via his GitHub profile.
