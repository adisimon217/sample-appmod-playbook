using FraudWatch.Core.Interfaces;
using FraudWatch.Core.Services;
using FraudWatch.Infrastructure.Caching;
using FraudWatch.Infrastructure.Data;
using FraudWatch.Infrastructure.ExternalApis;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/fraudwatch-.log", rollingInterval: RollingInterval.Day);
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FraudWatch API",
        Version = "v1",
        Description = "Real-time fraud detection and alerting service"
    });
});

// Redis distributed cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "FraudWatch:";
});

// Typed HttpClient for Banking API
builder.Services.AddHttpClient<IBankingApiClient, BankingApiClient>(client =>
{
    var bankingApiUrl = builder.Configuration["BankingApi:BaseUrl"]
        ?? throw new InvalidOperationException("BankingApi:BaseUrl not configured");
    client.BaseAddress = new Uri(bankingApiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Core services
builder.Services.AddScoped<IFraudDetectionService, FraudDetectionService>();
builder.Services.AddScoped<IRiskScoringEngine, RiskScoringEngine>();
builder.Services.AddScoped<IRuleEvaluator, RuleEvaluator>();

// Infrastructure services
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IFraudAlertRepository, FraudAlertRepository>();

// Health checks
builder.Services.AddHealthChecks()
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379")
    .AddUrlGroup(new Uri(builder.Configuration["BankingApi:BaseUrl"] + "/health"), "banking-api");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
