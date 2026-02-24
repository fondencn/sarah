# Sarah – Service Reference

Details on every microservice in the Sarah platform: responsibilities, endpoints, databases, and dependencies.

## Microservices Overview

| Service | Port | Purpose | Database |
|---------|------|---------|----------|
| Device Service | 5001 | Z-Wave & LoRaWAN device control | `devicesdb` |
| Persons Service | 5002 | Person/presence tracking | `personsdb` |
| Geofences Service | 5003 | Location-based automation | `geofencesdb` |
| Room Service | 5004 | Room management | `roomsdb` |
| Monitoring Service | 5005 | Weather monitoring, system metrics | `monitoringdb` |
| Rules Service | 5006 | Condition-based automation rules | `rulesdb` |
| Dashboard Service | 5007 | Dashboard widget persistence | `dashboarddb` |
| Speech Server | 5008 | Voice recognition & TTS | – |

---

## Device Service (Port 5001)

**Purpose**: Manages all smart home devices and their states.

**Key Responsibilities**:
- Device discovery and registration
- Device state management (on/off, brightness, temperature)
- Z-Wave network communication
- Device grouping and associations
- LED animations

**Key Endpoints**:
- `GET /api/devices/lamps` – Retrieve all lamp devices
- `GET /api/devices/sensors` – Retrieve all sensors
- `GET /api/devices/{id}` – Get specific device details
- `GET /api/devices/status` – Service health check

**Dependencies**: Keycloak (JWT), RabbitMQ, `Sarah.API`, `Sarah.LEDService`, `Sarah.Messaging.RabbitMQ`

**OpenAPI**: http://localhost:5001/swagger

---

## Persons Service (Port 5002)

**Purpose**: Manages users, persons, and their presence/availability.

**Key Responsibilities**:
- User profile management
- Person tracking and availability status
- Presence detection integration
- User preferences and settings

**Key Use Cases**:
- Track who is home/away for automation rules
- Manage user preferences for device control
- Provide presence context for geofencing

**Dependencies**: Keycloak (JWT), RabbitMQ

**OpenAPI**: http://localhost:5002/swagger

---

## Geofences Service (Port 5003)

**Purpose**: Location-based automation and boundary detection.

**Key Responsibilities**:
- Geofence boundary definitions
- Location tracking and position updates
- Entry/exit event detection
- Home zone management

**Key Endpoints**:
- `GET /api/geofences` – List all geofences
- `GET /api/geofences/current?latitude={lat}&longitude={lon}` – Check current location
- `GET /api/geofences/home` – Get home geofence

**Use Case Flow**:
1. GPS tracker publishes location update
2. Geofences Service receives location
3. Checks if location crosses any boundaries
4. Publishes `PersonGeoFenceMessage` to RabbitMQ
5. Rules Service consumes event and triggers automation

**Dependencies**: Keycloak (JWT), RabbitMQ, Persons Service

**OpenAPI**: http://localhost:5003/swagger

---

## Room Service (Port 5004)

**Purpose**: Room and space organisation.

**Key Responsibilities**:
- Room definitions and hierarchies
- Device-to-room assignments
- Zone management

**Dependencies**: Keycloak (JWT), Device Service

**OpenAPI**: http://localhost:5004/swagger

---

## Monitoring Service (Port 5005)

**Purpose**: System health, metrics, and weather data.

**Key Responsibilities**:
- Service health monitoring
- Performance metrics collection
- System diagnostics
- Weather data integration

**Key Endpoints**:
- `GET /api/monitoring/status` – Service health check

**Dependencies**: Keycloak (JWT), RabbitMQ

**OpenAPI**: http://localhost:5005/swagger

---

## Rules Service (Port 5006)

**Purpose**: Condition-based automation engine and rule execution.

**Key Responsibilities**:
- Rule definition and storage
- Event pattern matching
- Condition evaluation (time, location, device state)
- Action execution (trigger devices, send email notifications)

**Typical Rule Flow**:
```
Event (e.g. Geofence Exit)
  → Rules Service evaluates conditions
  → Matches rule: "Turn off lights when leaving home"
  → Publishes command to Device Service
  → Device Service executes action
```

**Dependencies**: Keycloak (JWT), RabbitMQ, all other services (as action targets)

**OpenAPI**: http://localhost:5006/swagger

---

## Dashboard Service (Port 5007)

**Purpose**: Persists user-defined dashboard widget layouts.

**Dependencies**: Keycloak (JWT)

**OpenAPI**: http://localhost:5007/swagger

---

## Speech Server (Port 5008)

**Purpose**: Voice command recognition and text-to-speech synthesis.

**Key Responsibilities**:
- Voice command recognition
- Natural language processing
- Text-to-speech synthesis
- Audio playback management

**Use Case Example**:
```
User: "Turn on living room lights"
  → Speech Server processes command
  → Identifies intent: device control
  → Publishes device command to RabbitMQ
  → Device Service receives and executes
  → Speech Server confirms: "Living room lights are now on"
```

**Dependencies**: Keycloak (JWT), RabbitMQ, Device Service

**OpenAPI**: http://localhost:5008/swagger

---

## Shared Libraries

### Sarah.API
Core business objects, interfaces, and DTOs shared across all services:
- **Interfaces**: `IDeviceService`, `IRuleService`, `IGeoFenceService`
- **Business Objects**: `NetworkElement`, `AssociationGroup`, `SensorData`
- **DTOs**: Device models, request/response objects

### Sarah.Authentication
Centralised JWT authentication configuration:
- Keycloak integration via service discovery
- JWT token validation
- Environment-aware HTTPS metadata handling

### Sarah.Messaging.RabbitMQ
Event messaging infrastructure:
- **Base class**: `AbstractMessage`
- **Message types**: `PersonGeoFenceMessage`, `SayMessage`, `WeatherEventMessages`, `StopAudioMessage`
- **Topics**: Defined as constants in `MessageTopics`

### Sarah.ServiceClients
Typed HTTP client wrappers for inter-service calls: `DeviceServiceClient`, `PersonServiceClient`, etc.

---

## Service Dependencies Matrix

| Service | Database | RabbitMQ | Keycloak | Other Services |
|---------|----------|----------|----------|----------------|
| Device Service | devicesdb | ✅ Pub/Sub | ✅ Auth | – |
| Persons Service | personsdb | ✅ Pub/Sub | ✅ Auth | – |
| Geofences Service | geofencesdb | ✅ Pub/Sub | ✅ Auth | Persons (data) |
| Room Service | roomsdb | – | ✅ Auth | Device (assigns) |
| Monitoring Service | monitoringdb | ✅ Pub/Sub | ✅ Auth | All (monitors) |
| Rules Service | rulesdb | ✅ Pub/Sub | ✅ Auth | All (triggers) |
| Dashboard Service | dashboarddb | – | ✅ Auth | – |
| Speech Server | – | ✅ Pub/Sub | ✅ Auth | Device (controls) |

---

## Database Migrations

EF Core migrations run automatically on startup. To add a migration:

```bash
dotnet ef migrations add <MigrationName> --project Microservices/Sarah.<Service>.WebApi/
```

---

## API Documentation

Each service exposes OpenAPI/Swagger documentation when running:

| Service | Swagger UI |
|---------|-----------|
| Device Service | http://localhost:5001/swagger |
| Persons Service | http://localhost:5002/swagger |
| Geofences Service | http://localhost:5003/swagger |
| Room Service | http://localhost:5004/swagger |
| Monitoring Service | http://localhost:5005/swagger |
| Rules Service | http://localhost:5006/swagger |
| Dashboard Service | http://localhost:5007/swagger |
| Speech Server | http://localhost:5008/swagger |
