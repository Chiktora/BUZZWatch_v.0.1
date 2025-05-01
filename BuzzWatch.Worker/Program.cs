using BuzzWatch.Application;
using BuzzWatch.Infrastructure;
using BuzzWatch.Worker.Extensions;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Services.UseSerilog();

// Add application services
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication();

// Add Worker services (including Quartz)
builder.Services.AddWorkerServices(builder.Configuration);

// Build and run the host
var host = builder.Build();
await host.RunAsync();