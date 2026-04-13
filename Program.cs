using webhook_processing_platform.Application.Handlers;
using webhook_processing_platform.Application.Mappers;
using webhook_processing_platform.Application.Interfaces;
using webhook_processing_platform.Infrastructure.Repositories;
using webhook_processing_platform.Infrastructure.Validators;
using webhook_processing_platform.Infrastructure.Queues;
using System.Data;
using Npgsql;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register database connection with connection pooling and IPv4-only DNS resolution
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var connStrBuilder = new NpgsqlConnectionStringBuilder(connectionString);
var hostname = connStrBuilder.Host;

// Resolve hostname to IPv4 address to avoid IPv6 connectivity issues
try
{
    var addresses = Dns.GetHostAddresses(hostname);
    var ipv4Address = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

    if (ipv4Address != null)
    {
        connStrBuilder.Host = ipv4Address.ToString();
        Console.WriteLine($"Resolved {hostname} to IPv4: {ipv4Address}");
    }
    else
    {
        Console.WriteLine($"Warning: No IPv4 address found for {hostname}. Using original hostname.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"DNS resolution error for {hostname}: {ex.Message}. Using original hostname.");
}

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connStrBuilder.ConnectionString);
dataSourceBuilder.ConnectionStringBuilder.IncludeErrorDetail = true;

var dataSource = dataSourceBuilder.Build();

// Register data source as singleton for connection pooling
builder.Services.AddSingleton(dataSource);

builder.Services.AddScoped<IDbConnection>(sp =>
    sp.GetRequiredService<NpgsqlDataSource>().CreateConnection());

// Register repositories
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

// Register queue (in-memory for development, Redis for production)
builder.Services.AddSingleton<IWebhookQueue, InMemoryWebhookQueue>();

// Register handlers
builder.Services.AddScoped<IIncomingEventHandler, IncomingEventHandler>();

// Register background service for processing queued webhooks
builder.Services.AddHostedService<WebhookProcessingBackgroundService>();

// Register mappers
builder.Services.AddScoped<IncomingEventToPaymentMapper>();

// Register validators
builder.Services.AddScoped<ISignatureValidator, SignatureValidator>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Ensure data source is disposed on shutdown
await dataSource.DisposeAsync();
