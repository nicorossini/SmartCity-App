using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using SmartCity.Interfaces.Models;

namespace SmartCity.Services;

public class RabbitMQService : IRabbitMQService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    private const string EXCHANGE_NAME = "water_distribution_exchange";
    private const string QUEUE_NAME = "water_alerts_queue";

    public RabbitMQService()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;

        _channel.ExchangeDeclareAsync(EXCHANGE_NAME, ExchangeType.Fanout);
        _channel.QueueDeclareAsync(QUEUE_NAME, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBindAsync(QUEUE_NAME, EXCHANGE_NAME, "");
    }

    public async Task PublishAlertAsync(WaterAlert waterAlertEvent)
    {
        var json = JsonConvert.SerializeObject(waterAlertEvent);
        var body = Encoding.UTF8.GetBytes(json);
        var props = new BasicProperties();

        await _channel.BasicPublishAsync(EXCHANGE_NAME, "", false, props, body);
    }
}
