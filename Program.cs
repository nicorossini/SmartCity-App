using Microsoft.OpenApi.Models;
using Orleans;
using Orleans.Runtime;
using Orleans.Configuration;
using Orleans.Hosting;
using SmartCity.Grains;
using SmartCity.Services;
using SmartCity.Interfaces;
using StackExchange.Redis;
using MongoDB.Driver;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        // Configure Swagger/OpenAPI
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Water Distribution Service API",
                Version = "v1",
                Description = "Smart City Water Distribution Management System",
                Contact = new OpenApiContact
                {
                    Name = "Water Management Team",
                    Email = "water@smartcity.com"
                }
            });

            // Include XML comments for API documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // Register infrastructure services
        builder.Services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(builder.Configuration.GetConnectionString("MongoDB") ?? "mongodb://localhost:27017"));

        builder.Services.AddSingleton<IMongoDatabase>(sp =>
            sp.GetRequiredService<IMongoClient>().GetDatabase("WaterDistributionDB"));

        builder.Services.AddSingleton<IMongoDBService, MongoDbService>();

        // Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")
                ?? "localhost:6379"));

        builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

        // RabbitMQ
        builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();


        // Configure Orleans
        builder.Host.UseOrleans((context, siloBuilder) =>
        {

            siloBuilder
                .UseLocalhostClustering()
                .ConfigureLogging(logging => logging.AddConsole())
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "water-distribution-cluster";
                    options.ServiceId = "WaterDistributionService";
                })
                .Configure<SiloOptions>(options =>
                {
                    options.SiloName = Environment.MachineName;
                });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Water Distribution Service API v1");
                c.RoutePrefix = "swagger";
                c.DocumentTitle = "Water Distribution API";
            });
        }

        app.UseHttpsRedirection();
        //app.UseAuthorization();
        app.MapControllers();

        // Start application in backgrounf and then initialize sensors
        var runTask = app.RunAsync();

        // Initialize test data on startup
        _ = Task.Run(async () =>
        {
            // Wait for Orleans to be ready
            await Task.Delay(TimeSpan.FromSeconds(10));

            var clusterClient = app.Services.GetRequiredService<IClusterClient>();

            var managerGrain = clusterClient.GetGrain<IWaterManagerGrain>("water-system");
            await managerGrain.InitializeTestDataAsync();
        });

        await runTask;
    }
}
