using SMSRateLimiter.API.Middleware;
using SMSRateLimiter.Core.Configuration;
using SMSRateLimiter.Core.Interfaces;
using SMSRateLimiter.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        builder => builder
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c => 
{
    c.SwaggerDoc("v1", new() { Title = "SMS Rate Limiter API", Version = "v1" });
    
    // Enable XML Documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if(File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var rateLimiterConfig = new RateLimiterConfig();
builder.Configuration.GetSection("RateLimiter").Bind(rateLimiterConfig);

builder.Services.AddSingleton(rateLimiterConfig);
builder.Services.AddSingleton<ITokenBucketProvider, LocalTokenBucketProvider>();
builder.Services.AddSingleton<IRateLimiterService, RateLimiterService>();
builder.Services.AddHostedService(sp => (LocalTokenBucketProvider)sp.GetRequiredService<ITokenBucketProvider>());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SMS Rate Limiter API v1");
        c.RoutePrefix = string.Empty; 
    });
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseStaticFiles();
app.UseRouting(); 
app.UseCors("AllowAngularApp");


app.MapControllers();

app.Run();