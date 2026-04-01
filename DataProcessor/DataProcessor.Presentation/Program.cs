using DataProcessor.Application;
using DataProcessor.Infrastructure;
using DataProcessor.Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kafka settings from environment variables
var kafkaBrokers = Environment.GetEnvironmentVariable("KAFKA_BROKERS");
if (!string.IsNullOrEmpty(kafkaBrokers))
    builder.Configuration["Kafka:Brokers"] = kafkaBrokers;

var kafkaTopic = Environment.GetEnvironmentVariable("KAFKA_TOPIC");
if (!string.IsNullOrEmpty(kafkaTopic))
    builder.Configuration["Kafka:Topic"] = kafkaTopic;

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<ParameterGrpcService>();
app.MapGet("/", () => "DataProcessor gRPC service is running. Use a gRPC client to communicate.");

app.Run();
