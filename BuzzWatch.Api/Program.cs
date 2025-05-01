using BuzzWatch.Application;
using BuzzWatch.Application.Measurements.Commands;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Infrastructure;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddInfrastructure(builder.Configuration, useSqliteForTests: false)
    .AddApplication();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// API endpoints - v1 group
var v1 = app.MapGroup("/api/v1");

// POST /api/v1/devices/{deviceId}/measurements
v1.MapPost("/devices/{deviceId:guid}/measurements", 
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

// Bonus: GET /api/v1/devices
v1.MapGet("/devices", async (IMediator mediator, CancellationToken ct) =>
{
    // For now, just return an empty list
    return Results.Ok(new List<object>());
})
.WithName("GetDevices")
.Produces(200);

app.Run();

// This is needed for WebApplicationFactory in integration tests
namespace BuzzWatch.Api
{
    public class Program { }
}
