# BuzzWatch Integration Implementation Plan

## Phase 1: API Endpoints Completion (2 days)

### Day 1: Complete Device and Measurement API Endpoints

1. **Complete Device API Endpoints**
   ```csharp
   // GET /api/v1/devices - Replace placeholder with actual implementation
   v1.MapGet("/devices", 
       [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
       async (IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>
   {
       var query = new GetUserDevicesQuery(user.FindFirstValue(ClaimTypes.NameIdentifier));
       var devices = await mediator.Send(query, ct);
       return Results.Ok(devices);
   });
   
   // GET /api/v1/devices/{id}
   v1.MapGet("/devices/{id:guid}", 
       [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
       async (Guid id, IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>
   {
       var query = new GetDeviceQuery(id);
       var device = await mediator.Send(query, ct);
       return device is null ? Results.NotFound() : Results.Ok(device);
   });
   ```

2. **Enhance Measurement Repository for Large Datasets**
   ```csharp
   public async Task<List<MeasurementHeader>> GetByDeviceAsync(
       Guid deviceId, DateTimeOffset from, DateTimeOffset to, int limit = 1000, CancellationToken ct = default)
   {
       return await _db.Headers
           .Where(m => m.DeviceId == deviceId && m.RecordedAt >= from && m.RecordedAt <= to)
           .OrderByDescending(m => m.RecordedAt)
           .Take(limit)
           .Include(h => h.TempIn)
           .Include(h => h.HumIn)
           .Include(h => h.TempOut)
           .Include(h => h.HumOut)
           .Include(h => h.Weight)
           .ToListAsync(ct);
   }
   ```

3. **Add Export Endpoint to API**
   ```csharp
   // GET /api/v1/devices/{id}/export
   v1.MapGet("/devices/{id:guid}/export", 
       [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
       async (Guid id, [FromQuery] int days, IMediator mediator, CancellationToken ct) =>
   {
       var query = new ExportDeviceDataQuery(id, days);
       var exportData = await mediator.Send(query, ct);
       return exportData is null ? Results.NotFound() : Results.Ok(exportData);
   });
   ```

### Day 2: Configure SignalR for Real-time Updates

1. **Enhance MeasurementHub Implementation**
   ```csharp
   // Verify JWT token includes device access and add users to appropriate groups
   // Add proper logging and error handling
   ```

2. **Fix CORS Configuration for SignalR**
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowAll", builder =>
       {
           builder
               .AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
       });
       
       options.AddPolicy("SignalR", builder =>
       {
           builder
               .WithOrigins("https://localhost:7195")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials(); // Required for SignalR
       });
   });
   ```

3. **Test Real-time Updates End-to-End**
   - Create test script for simulating device measurements
   - Verify SignalR connections and message delivery

## Phase 2: Authentication and Security (2 days)

### Day 1: JWT Authentication Enhancements

1. **Implement Token Refresh Endpoint**
   ```csharp
   v1.MapPost("/auth/refresh", 
       async (RefreshTokenRequest req, UserManager<AppUser> um, IConfiguration cfg) =>
   {
       // Validate refresh token
       // Generate new JWT token
       // Return new token pair
   });
   ```

2. **Add Role-Based Authorization**
   ```csharp
   // Admin-only endpoints
   v1.MapGet("/admin/users", 
       [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
       async (IMediator mediator, CancellationToken ct) =>
   {
       var query = new GetAllUsersQuery();
       var users = await mediator.Send(query, ct);
       return Results.Ok(users);
   });
   ```

3. **Update ApiClient to Handle Token Refresh**
   ```csharp
   public async Task<HttpResponseMessage> SendWithAuthAsync(HttpRequestMessage request)
   {
       // Add JWT token
       // If 401, try refresh and retry
       // Handle refresh failures
   }
   ```

### Day 2: Device Authentication

1. **Enhance API Key Authentication**
   ```csharp
   // Implement rate limiting for API key auth
   // Add device verification against registered keys
   ```

2. **Implement Device Registration Workflow**
   ```csharp
   v1.MapPost("/devices/register", 
       [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
       async (RegisterDeviceRequest req, IMediator mediator, CancellationToken ct) =>
   {
       var command = new RegisterDeviceCommand(req.Name, req.Location);
       var result = await mediator.Send(command, ct);
       return Results.Created($"/api/v1/devices/{result.DeviceId}", result);
   });
   ```

3. **Add User-Device Access Control**
   ```csharp
   // Implement device sharing between users
   // Add device access policies
   ```

## Phase 3: Data Visualization and Export (2 days)

### Day 1: Chart Data Optimization

1. **Optimize Data Aggregation for Charts**
   ```csharp
   // Add data downsampling for time series
   // Implement caching for chart data
   ```

2. **Complete Excel Export with Statistics**
   ```csharp
   // Add variance calculations
   // Include trend analysis
   ```

3. **Add CSV/JSON Export Endpoints to API**
   ```csharp
   // Implement streaming response for large datasets
   ```

### Day 2: Dashboard Widgets

1. **Implement Widget State Persistence**
   ```csharp
   // Store widget preferences in user profile
   ```

2. **Add Weather API Integration**
   ```csharp
   // Connect to OpenWeather API
   // Map weather data to device locations
   ```

3. **Implement Analytics Dashboard**
   ```csharp
   // Add comparison charts
   // Create statistical summaries
   ```

## Phase 4: Testing and Deployment (2 days)

### Day 1: Comprehensive Testing

1. **Create Integration Tests for API**
   ```csharp
   // Test authentication flows
   // Test device access control
   // Test data export
   ```

2. **Create E2E Tests for Web UI**
   ```csharp
   // Test dashboard widgets
   // Test export functionality
   // Test real-time updates
   ```

3. **Add Performance Tests**
   ```csharp
   // Test with large datasets
   // Test SignalR scalability
   ```

### Day 2: Deployment Preparation

1. **Create Docker Compose Setup**
   ```yaml
   # Docker Compose file with:
   # - API
   # - Web
   # - Database
   # - Redis for caching
   ```

2. **Configure CI/CD Pipeline**
   ```yaml
   # GitHub Actions workflow
   ```

3. **Create Deployment Documentation**
   ```markdown
   # Deployment steps
   # Configuration options
   # Monitoring and maintenance
   ```

## Phase 5: Final Features and Polish (2 days)

### Day 1: Alert System

1. **Implement Alert Rules Engine**
   ```csharp
   // Define rule types
   // Add rule evaluation
   ```

2. **Add Email Notifications**
   ```csharp
   // Configure SMTP
   // Create email templates
   ```

3. **Create Alert History**
   ```csharp
   // Store alert history
   // Add alert dashboard
   ```

### Day 2: Final Polish

1. **Optimize Performance**
   ```csharp
   // Add caching
   // Optimize queries
   ```

2. **Enhance Error Handling**
   ```csharp
   // Add global exception handler
   // Improve error messages
   ```

3. **Add Documentation**
   ```markdown
   # User documentation
   # API documentation
   # Developer guide
   ``` 