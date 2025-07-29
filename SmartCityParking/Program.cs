using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartCityParking.Services.Interfaces;
using SmartCityParking.Services.Implementations; 
using SmartCityParking.Grains.Interfaces;         
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "SecretKeyNotSet";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Add custom services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService>(provider => 
    new AuthService(provider.GetRequiredService<IJwtService>()));
builder.Services.AddScoped<IMongoService, MongoService>();

// Configure Orleans
builder.Host.UseOrleans((context, silo) =>
{
    silo.UseLocalhostClustering(
        siloPort: 11111,
        gatewayPort: 30000,
        primarySiloEndpoint: null)
        .ConfigureLogging(logging => logging.AddConsole())
        .UseDashboard(options => 
        { 
            options.Host = "*";
            options.Port = 8080;
            options.HostSelf = true;
        })
        .AddMemoryGrainStorageAsDefault()
        .ConfigureServices(services =>
        {
            // Register grain services
            services.AddScoped<IMongoService, MongoService>();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Start app
await app.StartAsync();

try
{
    // Initialize parking system with a delay to ensure Orleans is ready
    await Task.Delay(2000); // Give Orleans time to fully initialize
    
    using (var scope = app.Services.CreateScope())
    {
        var grainFactory = scope.ServiceProvider.GetRequiredService<IGrainFactory>();
        var parkingManager = grainFactory.GetGrain<IParkingManagerGrain>("main");
        
        Console.WriteLine("Initializing parking system with 10 spots...");
        await parkingManager.InitializeAsync(10);
        Console.WriteLine("Parking system initialized successfully!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error initializing parking system: {ex.Message}");
}

// Keep the app running until shutdown
await app.WaitForShutdownAsync();
