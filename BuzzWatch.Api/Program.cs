using BuzzWatch.Api.Authentication;
using BuzzWatch.Api.Authorization;
using BuzzWatch.Application;
using BuzzWatch.Application.Measurements.Commands;
using BuzzWatch.Contracts.Auth;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Infrastructure;
using BuzzWatch.Infrastructure.Extensions;
using BuzzWatch.Infrastructure.Identity;
using BuzzWatch.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddInfrastructure(builder.Configuration, useSqliteForTests: false)
    .AddApplication();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddScoped<IAuthorizationHandler, OwnsDeviceHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Seed identity data in development
    await app.Services.SeedIdentityAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// API endpoints - v1 group
var v1 = app.MapGroup("/api/v1");

// POST /api/v1/auth/login - Get JWT token
v1.MapPost("/auth/login", 
    async (LoginRequest req, UserManager<AppUser> um, IConfiguration cfg) =>
{
    var user = await um.FindByEmailAsync(req.Email);
    if (user is null || !await um.CheckPasswordAsync(user, req.Password))
        return Results.Unauthorized();

    var roles = await um.GetRolesAsync(user);
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email!)
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var token = JwtGenerator.CreateToken(cfg, claims);
    return Results.Ok(new LoginResponse(token));
})
.WithName("Login")
.Produces<LoginResponse>(200)
.ProducesProblem(401);

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

// GET /api/v1/devices
v1.MapGet("/devices", 
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    async (IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>
{
    // This will be implemented properly later
    return Results.Ok(new List<object>());
})
.WithName("GetDevices")
.Produces(200);

app.Run();

// This is needed for WebApplicationFactory in integration tests
namespace BuzzWatch.Api
{
    public partial class Program { }
}
