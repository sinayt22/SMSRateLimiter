
using SMSRateLimiter.API.Middleware;
using SMSRateLimiter.Core.Configuration;
using SMSRateLimiter.Core.Interfaces;
using SMSRateLimiter.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var rateLimiterConfig = new RateLimiterConfig();
builder.Configuration.GetSection("RateLimiter").Bind(rateLimiterConfig);

builder.Services.AddSingleton(rateLimiterConfig);
builder.Services.AddSingleton<ITokenBucketProvider, LocalTokenBucketProvider>();
builder.Services.AddSingleton<IRateLimiterService, RateLimiterService>();
builder.Services.AddHostedService(sp => (LocalTokenBucketProvider)sp.GetRequiredService<ITokenBucketProvider>());


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();