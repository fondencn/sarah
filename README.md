# Sarah

Chris's Smart Home

## Overview

Sarah is a smart home management system designed to provide users with a seamless and intuitive way to control and monitor their smart home devices. The system includes features such as device management, user-specific dashboards, and extended property handling for various smart devices.

## Technologies Used

- **ASP.NET Core**: For building the web API.
- **Entity Framework Core**: For database interactions.
- **SQLLite**: As the database management system.
- **Angular**: For the front-end application.
- **IdentityServer4**: For authentication and authorization.
- **Serilog**: For logging.
- **Docker**: For containerization.

## Project Structure

The project is organized into several key directories and files:

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) (for the Angular front-end)
- [Docker](https://www.docker.com/) (for containerization)

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
- **User Authentication**: Secure login and registration using IdentityServer4.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any changes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE

For any questions or inquiries, please contact Chris via his github profile.
