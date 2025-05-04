using BuzzWatch.Api.Authentication;
using BuzzWatch.Api.Authorization;
using BuzzWatch.Api.Hubs;
using BuzzWatch.Api.Services;
using BuzzWatch.Api.Swagger;
using BuzzWatch.Application;
using BuzzWatch.Application.Abstractions;
using BuzzWatch.Application.Alerts.Commands;
using BuzzWatch.Application.Devices.Queries;
using BuzzWatch.Application.Measurements.Commands;
using BuzzWatch.Contracts.Auth;
using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Domain.Enums;
using BuzzWatch.Domain.Measurements;
using BuzzWatch.Infrastructure;
using BuzzWatch.Infrastructure.Abstractions;
using BuzzWatch.Infrastructure.Extensions;
using BuzzWatch.Infrastructure.Identity;
using BuzzWatch.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddInfrastructure(builder.Configuration, useSqliteForTests: false)
    .AddApplication();

// Add controllers for AdminController
builder.Services.AddControllers().AddJsonOptions(options => {
    // Use appropriate JSON naming policy
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // Avoid ignoring null values so they appear in Swagger docs
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
});

// Add SignalR
builder.Services.AddSignalR();

// Register notification service
builder.Services.AddScoped<IMeasurementNotificationService, SignalRMeasurementNotificationService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add extremely minimal Swagger configuration
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
        Title = "BuzzWatch API",
        Version = "v1",
    });
    
    // Minimal unique schema IDs
    options.CustomSchemaIds(type => {
        if (type.FullName == null) 
            return type.Name;
            
        return type.Namespace + "." + type.Name;
    });
    
    // Resolve conflicting actions
    options.ResolveConflictingActions(apiDescriptions => {
        var resolver = new ConflictingActionsResolver();
        return resolver.Resolve(apiDescriptions);
    });
});

// Enable more detailed Swagger exceptions
builder.Services.AddSwaggerGen().AddLogging(logging => {
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// Add HttpContext accessor for authorization
builder.Services.AddHttpContextAccessor();

// Configure authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.ConfigureJwtOptions(builder.Configuration))
    .AddApiKeySupport();

// Configure authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OwnsDevice", policy => policy.Requirements.Add(new OwnsDeviceRequirement()));
});

// Add CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins("https://localhost:7093", "https://localhost:7195")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<IAuthorizationHandler, OwnsDeviceHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Simple exception logging middleware for Swagger
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                Console.WriteLine($"Swagger error: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync($"Swagger error: {ex.Message}");
                return;
            }
            throw;
        }
    });

    // Configure basic Swagger
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Seed identity data in development
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    
    try 
    {
        // Seed identity data (users and roles)
        await services.SeedIdentityAsync();
        
        // Seed device data
        await services.SeedDevicesAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database");
    }
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

// Add this line to register controller-based endpoints
app.MapControllers();

// Map SignalR hub
app.MapHub<MeasurementHub>("/hubs/measurements");

// API endpoints - v1 group
var v1 = app.MapGroup("/api/v1");

// POST /api/v1/auth/login - Get JWT token
v1.MapPost("/auth/login", 
    async (LoginRequest req, UserManager<AppUser> um, IConfiguration cfg) =>
{
    var user = await um.FindByEmailAsync(req.Email);
    
    // Check if user exists and password is correct
    if (user is null || !await um.CheckPasswordAsync(user, req.Password))
        return Results.Unauthorized();
    
    // Check if account is active
    if (!user.IsActive)
        return Results.Forbid(); // Return 403 Forbidden for inactive accounts
    
    var roles = await um.GetRolesAsync(user);
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email!)
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var token = JwtGenerator.CreateToken(cfg, claims);
    // Include the user's role in the response
    string? userRole = roles.FirstOrDefault();
    return Results.Ok(new LoginResponse(token, userRole));
})
.WithName("Login")
.Produces<LoginResponse>(200)
.ProducesProblem(401)
.ProducesProblem(403);

// POST /api/v1/devices/{deviceId}/measurements
v1.MapPost("/devices/{deviceId:guid}/measurements", 
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.Scheme)]
    async (Guid deviceId,
           MeasurementCreateRequest dto,
           IMediator mediator,
           CancellationToken ct) =>
{
    var cmd = new CreateMeasurementCommand(
        deviceId,
        dto.RecordedAt,
        dto.TempInsideC,
        dto.HumInsidePct,
        dto.TempOutsideC,
        dto.HumOutsidePct,
        dto.WeightKg);
        
    var result = await mediator.Send(cmd, ct);
    
    return Results.Created($"/api/v1/measurements/{result}", new { id = result });
})
.WithName("CreateMeasurement")
.Produces(201)
.ProducesProblem(400);

// GET /api/v1/devices/{id}
v1.MapGet("/devices/{id:guid}", 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var query = new GetDeviceQuery(id);
    var device = await mediator.Send(query, ct);
    return device is null ? Results.NotFound() : Results.Ok(device);
})
.WithName("GetDevice")
.Produces<DeviceDto>(200)
.ProducesProblem(404);

// GET /api/v1/devices/{id}/measurements
v1.MapGet("/devices/{id:guid}/measurements", 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    async (Guid id, [FromQuery] int? limit, [FromQuery] int? days, [FromQuery] bool? downsampled, IMediator mediator, CancellationToken ct) =>
{
    // Use the repository directly for simplicity
    using var scope = app.Services.CreateScope();
    var measurementRepo = scope.ServiceProvider.GetRequiredService<IMeasurementRepository>();
    var dateProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
    
    // Calculate date range
    int daysToRetrieve = days ?? 7;
    var to = dateProvider.UtcNow;
    var from = to.AddDays(-daysToRetrieve);
    
    // Get measurements
    List<MeasurementHeader> measurements;
    if (downsampled.GetValueOrDefault())
    {
        // For chart display, use chunked/downsampled data
        measurements = await measurementRepo.GetByDeviceChunkedAsync(id, from, to, 60, ct);
    }
    else
    {
        // For table display, use regular data with limit
        int limitValue = limit ?? 100;
        measurements = await measurementRepo.GetByDeviceAsync(id, from, to, limitValue, ct);
    }
    
    // Map to DTOs
    var result = measurements.Select(m => new MeasurementDto
    {
        Id = m.Id,
        DeviceId = m.DeviceId,
        Timestamp = m.RecordedAt,
        TempInsideC = m.TempIn?.ValueC,
        HumInsidePct = m.HumIn?.ValuePct,
        TempOutsideC = m.TempOut?.ValueC,
        HumOutsidePct = m.HumOut?.ValuePct,
        WeightKg = m.Weight?.ValueKg,
        BatteryPct = 100 // Placeholder
    }).ToList();
    
    return Results.Ok(result);
})
.WithName("GetDeviceMeasurements")
.Produces<List<MeasurementDto>>(200)
.ProducesProblem(404);

// GET /api/v1/devices/{id}/export
v1.MapGet("/devices/{id:guid}/export", 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    async (Guid id, [FromQuery] int days, IMediator mediator, CancellationToken ct) =>
{
    var query = new ExportDeviceDataQuery(id, days);
    var exportData = await mediator.Send(query, ct);
    return exportData is null ? Results.NotFound() : Results.Ok(exportData);
})
.WithName("ExportDeviceData")
.Produces<DeviceExportDataDto>(200)
.ProducesProblem(404);

// POST /api/v1/devices/{deviceId}/alerts
v1.MapPost("/devices/{deviceId:guid}/alerts", 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    async (Guid deviceId,
           EndpointCreateAlertRuleRequest req,
           IMediator mediator,
           CancellationToken ct) =>
{
    var cmd = new CreateAlertRuleCommand
    {
        DeviceId = deviceId,
        Metric = req.Metric,
        Operator = Enum.Parse<BuzzWatch.Domain.Alerts.ComparisonOperator>(req.Operator),
        Threshold = req.Threshold,
        DurationSeconds = req.DurationSeconds
    };
    
    var ruleId = await mediator.Send(cmd, ct);
    
    return Results.Created($"/api/v1/alerts/{ruleId}", new { id = ruleId });
})
.WithName("CreateAlertRule")
.Produces(201)
.ProducesProblem(400);

app.Run();

// This is needed for WebApplicationFactory in integration tests
namespace BuzzWatch.Api
{
    public partial class Program { }
}

// Record for alert rule creation request
public record EndpointCreateAlertRuleRequest(
    string Metric,
    string Operator,
    decimal Threshold,
    int DurationSeconds);
