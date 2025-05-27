using StackExchange.Redis;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using SmartCity.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();

        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect("localhost:6379")
        );
        builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();


        builder.Host.UseOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering();

            siloBuilder.UseMongoDBClient("mongodb://localhost:27017")
                .AddMongoDBGrainStorage("mongoStore", options =>
                {
                    options.DatabaseName = "SmartCityDb";
                });
        });

        var app = builder.Build();

        //app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
