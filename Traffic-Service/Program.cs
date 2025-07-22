using MongoDB.Driver;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using StackExchange.Redis;
using System.Text;
using Traffic_Service.Interfaces;
using Traffic_Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT - problems with package installation
/*
// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SmartCityIssuer",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SmartCityAudience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForJWTAuthentication123456789"))
        };
    });
*/

// MongoDB
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetConnectionString("MongoDB")
        ?? "mongodb://localhost:27017"));
builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetService<IMongoClient>()!.GetDatabase("SmartCityTraffic"));
builder.Services.AddSingleton<IMongoDbService, MongoDbService>();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")
        ?? "localhost:6379"));
builder.Services.AddSingleton<IRedisService, RedisService>();

// RabbitMQ
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// Orleans
builder.Host.UseOrleans((context, siloBuilder) =>
{
    siloBuilder
        .UseLocalhostClustering()
        .ConfigureLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information))
        .UseDashboard(options => { })
        // Configuaration for stability
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "dev";
            options.ServiceId = "TrafficService";
        })
        .ConfigureServices(services =>
        {
            // Future Configuration if are necessary
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// JWT
// app.UseAuthentication();
// app.UseAuthorization();

app.MapControllers();

// Endpoint for health check
app.MapGet("/health", () => "Healthy");

// Start application in backgrounf and then initialize sensors
var runTask = app.RunAsync();

await Task.Delay(5000);

// Initialize some test sensors on startup after that Orleans is rwady
_ = Task.Run(async () =>
{
    await InitializeTestSensorsWithRetry(app.Services);
});

await runTask;

// Helper method fot initialize tests sensor with retry
static async Task InitializeTestSensorsWithRetry(IServiceProvider services)
{
    const int maxRetries = 5;
    const int delayBetweenRetries = 2000;

    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            var grainFactory = scope.ServiceProvider.GetRequiredService<IGrainFactory>();

            // Check that Orleans is ready
            var managementGrain = grainFactory.GetGrain<IManagementGrain>(0);
            var siloStatuses = await managementGrain.GetHosts();

            if (!siloStatuses.Any())
            {
                throw new InvalidOperationException("No active silos available");
            }

            var trafficManager = grainFactory.GetGrain<ITrafficManagerGrain>(0);

            // Register some test sensors
            await trafficManager.RegisterSensorAsync("SENSOR_001", "Via Roma - Piazza Centrale");
            await trafficManager.RegisterSensorAsync("SENSOR_002", "Corso Italia - Via Garibaldi");
            await trafficManager.RegisterSensorAsync("SENSOR_003", "Viale Europa - Via Nazionale");
            await trafficManager.RegisterSensorAsync("SENSOR_004", "Piazza Duomo - Via Mazzini");
            await trafficManager.RegisterSensorAsync("SENSOR_005", "Stazione Centrale - Via Veneto");

            Console.WriteLine("Test sensors initialized successfully!");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Attempt {attempt}/{maxRetries} - Error initializing test sensors: {ex.Message}");

            if (attempt == maxRetries)
            {
                Console.WriteLine("Failed to initialize test sensors after all retries. The application will continue without test data.");
                return;
            }

            await Task.Delay(delayBetweenRetries);
        }
    }
}