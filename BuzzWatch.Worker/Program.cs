using BuzzWatch.Application;
using BuzzWatch.Infrastructure;
using BuzzWatch.Worker.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Use Serilog for logging
builder.Services.AddLogging(loggingBuilder =>
    loggingBuilder.AddSerilog(dispose: true));

// Add services
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddWorkerServices(builder.Configuration);

var host = builder.Build();
await host.RunAsync();