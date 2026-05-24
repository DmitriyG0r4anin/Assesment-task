using NotificationsService.Configuration;
using NotificationsService.Consumers;
using NotificationsService.Hubs;
using NotificationsService.Serialization;

var builder = WebApplication.CreateSlimBuilder(args);

var inContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);
if (!inContainer)
{
    builder.WebHost.UseKestrelHttpsConfiguration();
}

var services = builder.Services;

services.Configure<KafkaConfig>(builder.Configuration.GetSection(KafkaConfig.SectionName));

services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.TypeInfoResolver = SignalRPayloadJsonContext.Default;
});
services.AddHostedService<KafkaMotionConsumer>();

services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/health", () => "OK");

app.MapHub<MotionHub>("/notifications/motionHub");

await app.RunAsync();
