using DataProcessor.Application;
using DataProcessor.Infrastructure;
using DataProcessor.Presentation.Mapping;
using DataProcessor.Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication()
                .AddInfrastructure(builder.Configuration);

builder.Services.AddGrpc();

var app = builder.Build();

GrpcMappingConfig.RegisterMappings();

app.MapGrpcService<AirQualityGrpcService>();
app.MapGrpcService<EnergyGrpcService>();
app.MapGrpcService<MotionGrpcService>();
app.MapGrpcService<RoomGrpcService>();
app.MapGet("/", () => "DataProcessor gRPC service is running. Use a gRPC client to communicate.");

app.Run();
