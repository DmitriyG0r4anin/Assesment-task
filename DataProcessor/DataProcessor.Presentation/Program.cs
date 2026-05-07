using DataProcessor.Application;
using DataProcessor.Infrastructure;
using DataProcessor.Presentation.Mapping;
using DataProcessor.Presentation.Services;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddApplication()
    .AddInfrastructure(builder.Configuration);

services.AddGrpc();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

GrpcMappingConfig.RegisterMappings();

app.MapGrpcService<AirQualityGrpcService>();
app.MapGrpcService<EnergyGrpcService>();
app.MapGrpcService<MotionGrpcService>();
app.MapGrpcService<RoomGrpcService>();

app.MapGet("/", () => "DataProcessor gRPC service is running. Use a gRPC client to communicate.");

await app.RunAsync();
