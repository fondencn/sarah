# Dashboard Refactoring Summary

## Overview
This refactoring implements a new microservice architecture for dashboard functionality, separating concerns between dashboard configuration and actual data fetching.

## Changes Made

### 1. New Dashboard Microservice (Sarah.Dashboard.WebApi)

#### Purpose
- Stores dashboard item metadata (what items to display)
- Provides CRUD operations for dashboard items
- Does NOT call other microservices
- Does NOT aggregate data from other services

#### Key Features
- **Technology Stack**: ASP.NET Core 10.0, Entity Framework Core, PostgreSQL
- **Authentication**: JWT with Keycloak
- **Database**: Dedicated PostgreSQL instance (port 5438)
- **API Port**: 5007
- **Swagger**: Available at http://localhost:5007/swagger

#### Endpoints
```
GET    /api/Dashboard                      - Get all dashboard items
GET    /api/Dashboard/{id}                 - Get dashboard item by database ID
POST   /api/Dashboard                      - Create a new dashboard item
DELETE /api/Dashboard/{itemId}/{itemType} - Delete dashboard item by source ID and type
```

#### Data Model
**DashboardItemEntity** (Database)
- `Id` - Primary key (auto-generated)
- `ItemId` - ID from source system (device ID, person ID, etc.)
- `ItemType` - Type of item (Device, Scene, Room, Person, Weather)
- `Title` - Display title
- `Description` - Display description
- `Subtype` - Subcategory (e.g., "lamp", "wallplug")
- `ExtendedPropertiesJson` - JSON storage for metadata
- `CreatedAt` / `UpdatedAt` - Timestamps

**DashboardItemDto** (API)
- `ItemId` - Source system ID
- `ItemType` - Type enum
- `Title` - Display title
- `Description` - Display description
- `Subtype` - Subcategory
- `ExtendedProperties` - List of key-value pairs

#### Directory Structure
```
Sarah.Dashboard.WebApi/
├── Controllers/
│   └── DashboardController.cs
├── Data/
│   ├── DashboardDbContext.cs
│   ├── Entities/
│   │   └── DashboardItemEntity.cs
│   └── Repositories/
│       └── Repository.cs
├── DTOs/
│   ├── DashboardItemDto.cs
│   ├── CreateDashboardItemDto.cs
│   └── ExtendedPropertyDto.cs
├── Services/
│   └── DashboardService.cs
├── Migrations/
│   └── 20260208222916_InitialCreate.cs
├── Program.cs
├── Dockerfile
└── Sarah.Dashboard.WebApi.csproj
```

### 2. Frontend Changes

#### Updated Home Component (sarah.client/src/app/home/home.component.ts)

**New Architecture**:
1. **Load Phase**: 
   - Fetch dashboard items (metadata) from Dashboard service
   - For each device item, fetch actual device data from Device service concurrently using `Promise.all()`
   - Merge metadata with device data
   - Display combined data

2. **Update Phase**:
   - Periodically fetch dashboard items
   - Concurrently fetch fresh device data for all items using `Promise.all()`
   - Update UI with latest data
   - Timer only runs when user is logged in

**Key Methods**:
- `loadDashboardItems()` - Initial load with concurrent device data fetching
- `updateDashboardItems()` - Periodic updates with concurrent device data fetching
- `startDashboardUpdateTimer()` - Only starts if user is logged in

**Performance Improvements**:
- Changed from sequential to concurrent data fetching
- Uses `Promise.all()` for parallel HTTP requests
- Significantly reduces total load time

#### Generated TypeScript Client

Regenerated from new Dashboard service OpenAPI specification:
```
sarah.client/src/app/services/api-client/
├── api/
│   └── dashboard.service.ts (regenerated)
└── model/
    ├── dashboardItemDto.ts (updated)
    ├── dashboardItemType.ts (updated)
    ├── createDashboardItemDto.ts (new)
    ├── extendedPropertyDto.ts (updated)
    └── problemDetails.ts (new)
```

**Key Service Methods**:
- `apiDashboardGet()` - Get all dashboard items
- `apiDashboardPost(createDto)` - Create dashboard item
- `apiDashboardIdGet(id)` - Get dashboard item by ID
- `apiDashboardItemIdItemTypeDelete(itemId, itemType)` - Delete dashboard item

### 3. Infrastructure Updates

#### Docker Compose (docker-compose.microservices.yml)

**Added Services**:
```yaml
postgres-dashboard:
  image: postgres:16-alpine
  ports: ["5438:5432"]
  database: dashboarddb

dashboardservice:
  build: Microservices/Sarah.Dashboard.WebApi/Dockerfile
  ports: ["5007:8080"]
  depends_on: [postgres-dashboard, keycloak]
```

#### Solution File (Sarah.sln)
- Added `Sarah.Dashboard.WebApi.csproj` to solution

## Architecture Diagram

```
┌─────────────────┐
│   Browser UI    │
│  (Angular App)  │
└────────┬────────┘
         │
         ├─────────────────────────────────────┐
         │                                     │
         ▼                                     ▼
┌────────────────┐                   ┌────────────────┐
│   Dashboard    │                   │     Device     │
│   Service      │                   │    Service     │
│   (Port 5007)  │                   │   (Port 5001)  │
└────────┬───────┘                   └────────┬───────┘
         │                                     │
         │ Stores metadata                    │ Provides actual data
         │ (what to show)                     │ (device state, etc.)
         │                                     │
         ▼                                     ▼
┌────────────────┐                   ┌────────────────┐
│  postgres-     │                   │  postgres-     │
│  dashboard     │                   │  devices       │
│  (Port 5438)   │                   │  (Port 5432)   │
└────────────────┘                   └────────────────┘

Flow:
1. UI loads dashboard config from Dashboard Service
2. UI fetches actual device data from Device Service (concurrent)
3. UI merges and displays combined data
```

## Key Design Decisions

### 1. Separation of Concerns
- Dashboard service only stores configuration (what to show)
- UI orchestrates data fetching from multiple services
- Each service maintains its own responsibility

### 2. Performance Optimization
- Concurrent HTTP requests using `Promise.all()`
- Reduces total load time from O(n) to O(1) for n devices
- Example: 10 devices load in ~1s instead of ~10s

### 3. Data Enrichment Pattern
```typescript
// Load dashboard config
const items = await dashboardService.apiDashboardGet()

// Enrich with actual data (concurrent)
const enrichedItems = await Promise.all(
  items.map(async item => {
    if (item.itemType === DEVICE) {
      const device = await devicesService.devicesIdGet(item.itemId)
      return merge(item, device)
    }
    return item
  })
)
```

### 4. Authentication & Authorization
- All endpoints require JWT authentication
- Consistent with other microservices
- Uses Keycloak for identity management

## Testing Recommendations

### Unit Tests
```bash
# Test Dashboard service
cd Microservices/Sarah.Dashboard.WebApi
dotnet test

# Test Angular components
cd sarah.client
npm test
```

### Integration Tests
1. Start all services: `docker-compose -f docker-compose.microservices.yml up`
2. Test dashboard endpoints:
   ```bash
   # Create dashboard item
  curl -X POST http://localhost:5007/api/Dashboard \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "itemId": 1,
       "itemType": 0,
       "title": "Living Room Lamp",
       "subtype": "lamp"
     }'
   
   # Get all dashboard items
  curl http://localhost:5007/api/Dashboard \
     -H "Authorization: Bearer $TOKEN"
   
   # Delete dashboard item
  curl -X DELETE http://localhost:5007/api/Dashboard/1/0 \
     -H "Authorization: Bearer $TOKEN"
   ```

### UI Testing
1. Navigate to https://localhost:4200
2. Login with Keycloak credentials
3. Verify dashboard items load correctly
4. Check browser console for any errors
5. Verify device states update periodically

## Migration Guide

### For Existing Deployments

1. **Database Setup**:
   ```sql
   CREATE DATABASE dashboarddb;
   ```

2. **Run Migrations**:
   ```bash
   cd Microservices/Sarah.Dashboard.WebApi
   dotnet ef database update
   ```

3. **Populate Dashboard Items** (example):
   ```bash
   # Add your favorite devices to dashboard
  curl -X POST http://localhost:5007/api/Dashboard \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "itemId": 1,
       "itemType": 0,
       "title": "Living Room Lamp",
       "subtype": "lamp"
     }'
   ```

4. **Deploy Services**:
   ```bash
   docker-compose -f docker-compose.microservices.yml up -d
   ```

## Security

### Scan Results
- ✅ **CodeQL Analysis**: No vulnerabilities found
- ✅ **Code Review**: All issues addressed
- ✅ **Authentication**: JWT with Keycloak
- ✅ **HTTPS**: Supported (configure in production)

### Security Best Practices Applied
- Input validation on all endpoints
- Unique constraints on database (ItemId + ItemType)
- Proper error handling without information leakage
- JWT token validation on all endpoints
- CORS configured appropriately

## Future Enhancements

### Potential Improvements
1. **Real-time Updates**: Use SignalR for push notifications
2. **Caching**: Add Redis cache for frequently accessed dashboard items
3. **Pagination**: Add pagination for large dashboard lists
4. **Filtering**: Add filtering by item type, user preferences
5. **Ordering**: Allow users to customize dashboard item order
6. **Batch Operations**: Add endpoints for bulk create/delete
7. **User Preferences**: Store dashboard layout per user
8. **Analytics**: Track dashboard item usage

### Extensibility
The service is designed to be extended:
- Add new item types in `DashboardItemType` enum
- Add new properties in `ExtendedProperties`
- Create new endpoints for specialized operations
- Integrate with additional microservices

## Troubleshooting

### Common Issues

**Dashboard items not loading**:
- Check if Dashboard service is running (port 5007)
- Verify authentication token is valid
- Check browser console for errors

**Device data not enriching**:
- Verify Device service is running (port 5001)
- Check network tab for failed requests
- Ensure device IDs in dashboard match actual devices

**Database connection errors**:
- Verify PostgreSQL is running
- Check connection string in appsettings.json
- Ensure database migrations are applied

## Contributors
- Implementation: GitHub Copilot
- Review: fondencn

## Version
- Dashboard Service: v1.0.0
- Generated: 2026-02-08
