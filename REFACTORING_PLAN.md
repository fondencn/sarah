# Microservice Refactoring Plan
**Goal**: Achieve true microservice independence by removing compile-time dependencies

## Overview

This document provides a step-by-step plan to refactor the two critical cross-service dependencies:
1. Rules.WebApi → DeviceService.WebApi
2. Monitoring.WebApi → Rules.WebApi

## Phase 1: Rules → DeviceService Refactoring

### Current State
```
Rules.WebApi (compile-time dependency)
    ↓
DeviceService.WebApi
```

### Target State
```
Rules.WebApi 
    ↓ (HTTP API or RabbitMQ events)
DeviceService.WebApi
```

### Step-by-Step Implementation

#### Step 1: Create Device Command DTOs in Rules Service
**File**: `Microservices/Sarah.Rules.WebApi/DTOs/DeviceCommands/`

```csharp
// BlinkAnimationCommand.cs
public class BlinkAnimationCommand
{
    public string DeviceId { get; set; }
    public int DurationMs { get; set; }
    public string Color { get; set; }
}

// StartSceneCommand.cs
public class StartSceneCommand
{
    public string SceneName { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

// StopSceneCommand.cs
public class StopSceneCommand
{
    public string SceneName { get; set; }
}
```

#### Step 2: Create DeviceService HTTP Client in Rules Service
**File**: `Microservices/Sarah.Rules.WebApi/Clients/DeviceServiceClient.cs`

```csharp
public interface IDeviceServiceClient
{
    Task ExecuteBlinkAnimationAsync(BlinkAnimationCommand command);
    Task StartSceneAsync(StartSceneCommand command);
    Task StopSceneAsync(StopSceneCommand command);
}

public class DeviceServiceClient : IDeviceServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeviceServiceClient> _logger;

    public DeviceServiceClient(HttpClient httpClient, ILogger<DeviceServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ExecuteBlinkAnimationAsync(BlinkAnimationCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/devices/animations/blink",
                command
            );
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute blink animation");
            throw;
        }
    }

    public async Task StartSceneAsync(StartSceneCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/devices/scenes/start",
                command
            );
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start scene {SceneName}", command.SceneName);
            throw;
        }
    }

    public async Task StopSceneAsync(StopSceneCommand command)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/devices/scenes/stop",
                command
            );
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop scene {SceneName}", command.SceneName);
            throw;
        }
    }
}
```

#### Step 3: Register HTTP Client in Rules Service
**File**: `Microservices/Sarah.Rules.WebApi/Program.cs`

```csharp
// Add before builder.Build()
builder.Services.AddHttpClient<IDeviceServiceClient, DeviceServiceClient>(client =>
{
    var deviceServiceUrl = builder.Configuration["DeviceServiceUrl"] ?? "http://deviceservice:5001";
    client.BaseAddress = new Uri(deviceServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

**File**: `Microservices/Sarah.Rules.WebApi/appsettings.json`

```json
{
  "DeviceServiceUrl": "http://localhost:5001"
}
```

#### Step 4: Update Rule Actions to Use HTTP Client
**File**: `Microservices/Sarah.Rules.WebApi/Services/Actions/BlinkAction.cs`

```csharp
// OLD CODE (remove)
using Sarah.DeviceService.Model.Animations;

public class BlinkAction : IRuleAction
{
    private readonly BlinkAnimation _animation;
    
    public async Task Execute()
    {
        await _animation.Execute();
    }
}

// NEW CODE
using Sarah.Rules.WebApi.DTOs.DeviceCommands;
using Sarah.Rules.WebApi.Clients;

public class BlinkAction : IRuleAction
{
    private readonly IDeviceServiceClient _deviceClient;
    private readonly string _deviceId;
    private readonly int _durationMs;
    private readonly string _color;
    
    public BlinkAction(IDeviceServiceClient deviceClient, string deviceId, int durationMs, string color)
    {
        _deviceClient = deviceClient;
        _deviceId = deviceId;
        _durationMs = durationMs;
        _color = color;
    }
    
    public async Task Execute()
    {
        var command = new BlinkAnimationCommand
        {
            DeviceId = _deviceId,
            DurationMs = _durationMs,
            Color = _color
        };
        
        await _deviceClient.ExecuteBlinkAnimationAsync(command);
    }
}
```

**Repeat for**:
- `StartSceneAction.cs`
- `StopSceneAction.cs`
- `HardCodedRuleStore.cs` (update how actions are created)

#### Step 5: Create Device Animation Endpoints in DeviceService
**File**: `Microservices/Sarah.DeviceService.WebApi/Controllers/AnimationsController.cs`

```csharp
[ApiController]
[Route("api/devices/animations")]
[Authorize]
public class AnimationsController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ILogger<AnimationsController> _logger;

    public AnimationsController(IDeviceService deviceService, ILogger<AnimationsController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    [HttpPost("blink")]
    public async Task<IActionResult> ExecuteBlink([FromBody] BlinkAnimationCommand command)
    {
        try
        {
            var device = await _deviceService.GetDeviceByIdAsync(command.DeviceId);
            if (device == null)
                return NotFound($"Device {command.DeviceId} not found");

            // Execute blink animation logic here
            var animation = new BlinkAnimation(device, command.DurationMs, command.Color);
            await animation.ExecuteAsync();

            return Ok(new { Message = "Blink animation executed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute blink animation");
            return StatusCode(500, "Failed to execute animation");
        }
    }

    [HttpPost("scenes/start")]
    public async Task<IActionResult> StartScene([FromBody] StartSceneCommand command)
    {
        try
        {
            // Scene execution logic
            return Ok(new { Message = $"Scene {command.SceneName} started" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start scene");
            return StatusCode(500, "Failed to start scene");
        }
    }

    [HttpPost("scenes/stop")]
    public async Task<IActionResult> StopScene([FromBody] StopSceneCommand command)
    {
        try
        {
            // Scene stop logic
            return Ok(new { Message = $"Scene {command.SceneName} stopped" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop scene");
            return StatusCode(500, "Failed to stop scene");
        }
    }
}
```

**File**: `Microservices/Sarah.DeviceService.WebApi/DTOs/` (create matching DTOs)

```csharp
// Match the command DTOs from Rules service
public class BlinkAnimationCommand { /* same properties */ }
public class StartSceneCommand { /* same properties */ }
public class StopSceneCommand { /* same properties */ }
```

#### Step 6: Remove Project Reference
**File**: `Microservices/Sarah.Rules.WebApi/Sarah.Rules.WebApi.csproj`

```xml
<!-- REMOVE THIS LINE -->
<ProjectReference Include="..\Sarah.DeviceService.WebApi\Sarah.DeviceService.WebApi.csproj" />
```

#### Step 7: Update Docker Compose Configuration
**File**: `docker-compose.microservices.yml`

```yaml
services:
  rules:
    environment:
      - DeviceServiceUrl=http://deviceservice:5001
    depends_on:
      - deviceservice  # Add dependency
```

#### Step 8: Test
1. Build Rules service independently: `dotnet build Microservices/Sarah.Rules.WebApi`
2. Build DeviceService independently: `dotnet build Microservices/Sarah.DeviceService.WebApi`
3. Run both services
4. Test rule execution that triggers device actions
5. Verify HTTP calls are successful

## Phase 2: Monitoring → Rules Refactoring

### Current State
```
Monitoring.WebApi (compile-time dependency)
    ↓
Rules.WebApi
```

### Target State
```
Monitoring.WebApi 
    ↓ (HTTP API)
Rules.WebApi
```

### Step-by-Step Implementation

#### Step 1: Create Rules Status DTO in Monitoring Service
**File**: `Microservices/Sarah.Monitoring.WebApi/DTOs/RuleStatusDto.cs`

```csharp
public class RuleStatusDto
{
    public string Status { get; set; }
    public int ActiveRules { get; set; }
    public DateTime LastExecution { get; set; }
}
```

#### Step 2: Create Rules HTTP Client in Monitoring Service
**File**: `Microservices/Sarah.Monitoring.WebApi/Clients/RulesServiceClient.cs`

```csharp
public interface IRulesServiceClient
{
    Task<RuleStatusDto> GetStatusAsync();
}

public class RulesServiceClient : IRulesServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RulesServiceClient> _logger;

    public RulesServiceClient(HttpClient httpClient, ILogger<RulesServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RuleStatusDto> GetStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/rules/status");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<RuleStatusDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rules status");
            throw;
        }
    }
}
```

#### Step 3: Register HTTP Client in Monitoring Service
**File**: `Microservices/Sarah.Monitoring.WebApi/Program.cs`

```csharp
builder.Services.AddHttpClient<IRulesServiceClient, RulesServiceClient>(client =>
{
    var rulesServiceUrl = builder.Configuration["RulesServiceUrl"] ?? "http://rules:5006";
    client.BaseAddress = new Uri(rulesServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

#### Step 4: Update RuleMonitor to Use HTTP Client
**File**: `Microservices/Sarah.Monitoring.WebApi/Services/Monitors/RuleMonitor.cs`

```csharp
// OLD CODE (remove)
using Sarah.Rules;

public class RuleMonitor : IMonitor
{
    private readonly IRuleService _ruleService;
    
    public void CheckRules()
    {
        var status = _ruleService.GetStatus();
    }
}

// NEW CODE
using Sarah.Monitoring.WebApi.Clients;

public class RuleMonitor : IMonitor
{
    private readonly IRulesServiceClient _rulesClient;
    private readonly ILogger<RuleMonitor> _logger;
    
    public RuleMonitor(IRulesServiceClient rulesClient, ILogger<RuleMonitor> logger)
    {
        _rulesClient = rulesClient;
        _logger = logger;
    }
    
    public async Task CheckRulesAsync()
    {
        try
        {
            var status = await _rulesClient.GetStatusAsync();
            _logger.LogInformation("Rules status: {Status}, Active: {Count}", 
                status.Status, status.ActiveRules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check rules status");
        }
    }
}
```

#### Step 5: Ensure Rules Service Exposes Status Endpoint
**File**: `Microservices/Sarah.Rules.WebApi/Controllers/RulesController.cs`

```csharp
[HttpGet("status")]
[AllowAnonymous]  // For monitoring
public async Task<ActionResult<RuleStatusDto>> GetStatus()
{
    var status = new RuleStatusDto
    {
        Status = "Running",
        ActiveRules = await _ruleService.GetActiveRuleCountAsync(),
        LastExecution = await _ruleService.GetLastExecutionTimeAsync()
    };
    
    return Ok(status);
}
```

#### Step 6: Remove Project Reference
**File**: `Microservices/Sarah.Monitoring.WebApi/Sarah.Monitoring.WebApi.csproj`

```xml
<!-- REMOVE THIS LINE -->
<ProjectReference Include="..\Sarah.Rules.WebApi\Sarah.Rules.WebApi.csproj" />
```

#### Step 7: Update Docker Compose
**File**: `docker-compose.microservices.yml`

```yaml
services:
  monitoring:
    environment:
      - RulesServiceUrl=http://rules:5006
    depends_on:
      - rules  # Add dependency
```

#### Step 8: Test
1. Build Monitoring service independently
2. Build Rules service independently
3. Run both services
4. Test monitoring check
5. Verify HTTP calls work

## Phase 3: Alternative - Event-Driven Refactoring (Optional)

For truly async operations, consider using RabbitMQ events instead of HTTP:

### Rules → DeviceService via Events

**Rules publishes**:
```csharp
await _rabbitMQ.PublishAsync("device.animation.blink", new BlinkAnimationEvent { ... });
```

**DeviceService subscribes**:
```csharp
await _rabbitMQ.SubscribeAsync<BlinkAnimationEvent>("device.animation.*", async (msg) => {
    // Execute animation
});
```

**Benefits**:
- Complete decoupling
- Better scalability
- Resilience to service failures
- Natural retry mechanism

**Trade-offs**:
- Async-only (no immediate response)
- More complex debugging
- Requires RabbitMQ infrastructure

## Testing Strategy

### Unit Tests
- Mock IDeviceServiceClient in Rules tests
- Mock IRulesServiceClient in Monitoring tests
- Test HTTP client implementations with HttpMessageHandler mocks

### Integration Tests
- Start all services with docker-compose
- Test cross-service communication
- Verify error handling and timeouts

### Deployment Tests
1. Deploy DeviceService alone → Should work
2. Deploy Rules alone → Should work (without executing device actions)
3. Deploy Monitoring alone → Should work (with rules check failing gracefully)
4. Deploy all together → Full functionality

## Rollback Plan

If refactoring causes issues:
1. Git revert to last working commit
2. Restore project references temporarily
3. Fix issues
4. Retry refactoring with lessons learned

## Success Metrics

- ✅ Each service builds independently
- ✅ Each service runs independently
- ✅ Cross-service communication works via HTTP/Events
- ✅ No compile-time dependencies between services
- ✅ All tests pass
- ✅ Docker compose still works
- ✅ Swagger documentation updated

## Timeline

- **Day 1**: Phase 1 (Rules → DeviceService)
- **Day 2**: Phase 2 (Monitoring → Rules)
- **Day 3**: Testing and validation

## Resources Needed

- HTTP client documentation
- RabbitMQ documentation (if using events)
- Circuit breaker patterns (Polly)
- Service discovery patterns

## Next Steps After Refactoring

1. Implement circuit breakers for resilience
2. Add distributed tracing
3. Consider API Gateway
4. Add health checks for cross-service dependencies
5. Document service contracts (OpenAPI)
