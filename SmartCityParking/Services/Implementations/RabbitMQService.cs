using RabbitMQ.Client;
using System.Text;
using SmartCityParking.Grains.Interfaces;
using Newtonsoft.Json;

namespace SmartCityParking.Services.Interfaces
{
    public class RabbitMQService : IRabbitMQService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string EXCHANGE_NAME = "traffic_events";
        private const string QUEUE_NAME = "traffic_updates";

        public RabbitMQService()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(EXCHANGE_NAME, ExchangeType.Fanout);
            _channel.QueueDeclare(QUEUE_NAME, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(QUEUE_NAME, EXCHANGE_NAME, "");
        }

        public async Task PublishTrafficEventAsync(TrafficEvent trafficEvent)
        {
            var json = JsonConvert.SerializeObject(trafficEvent);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: EXCHANGE_NAME,
                routingKey: "",
                basicProperties: null,
                body: body
            );

            await Task.CompletedTask;
        }

        public Task StartConsumingAsync()
        {
            // Implement consumer logic here if needed
            return Task.CompletedTask;
        }
    }
}
