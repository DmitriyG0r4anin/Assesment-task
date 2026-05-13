using NotificationsService.Configuration;
using NotificationsService.Hubs;
using NotificationsService.Serialization;
using NotificationsService.Services;

var builder = WebApplication.CreateSlimBuilder(args);
builder.WebHost.UseKestrelHttpsConfiguration();

var services = builder.Services;

services.Configure<KafkaConfig>(builder.Configuration.GetSection(KafkaConfig.SectionName));

services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.TypeInfoResolver = SignalRPayloadJsonContext.Default;
});
services.AddHostedService<KafkaMotionConsumerService>();

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
