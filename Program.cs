using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Accessing Connection String
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var azureMonitorConnectionString = builder.Configuration.GetConnectionString("AzureMonitor");

// Custom metrics for the application
var greeterMeter = new Meter("OtPrGrYa.Example", "1.0.0");
var countGreetings = greeterMeter.CreateCounter<int>("greetings.count", description: "Counts the number of greetings");

// Custom ActivitySource for the application
var greeterActivitySource = new ActivitySource("OtPrGrJa.Example");
var tracingOtlpEndpoint = builder.Configuration["OTLP_ENDPOINT_URL"];
var otel = builder.Services.AddOpenTelemetry();
otel.UseAzureMonitor(options =>
{
    options.ConnectionString = azureMonitorConnectionString; // Set the connection string
});
otel.WithMetrics(metrics => metrics
    .AddMeter(greeterMeter.Name)
    .AddMeter("Microsoft.AspNetCore.Hosting")
    .AddMeter("Microsoft.AspNetCore.Server.Kestrel"));
otel.WithTracing(tracing =>
{
    tracing.AddSource(greeterActivitySource.Name);
});
var app = builder.Build();

app.MapGet("/", SendGreeting);

async Task<String> SendGreeting(ILogger<Program> logger)
{
    // Create a new Activity scoped to the method
    using var activity = greeterActivitySource.StartActivity("GreeterActivity");

    // Log a message
    logger.LogInformation("Sending greeting");

    // Increment the custom counter
    countGreetings.Add(1);

    // Add a tag to the Activity
    activity?.SetTag("greeting", "Welcome to My World");

    return "Welcome to My World";
}

// Configure the Prometheus scraping endpoint

app.Run();
