using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;

namespace Ponabri.Api.Services
{
    public interface IMessageProducer
    {
        void SendMessage<T>(T message, string queueName);
    }

    public class RabbitMQProducer : IMessageProducer
    {
        private readonly IConnection? _connection;
        private readonly IModel? _channel;

        public RabbitMQProducer()
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao conectar ao RabbitMQ: {ex.Message}");
                _connection = null; 
                _channel = null;
            }
        }

        public void SendMessage<T>(T message, string queueName)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                Console.WriteLine("Canal RabbitMQ não está disponível. Mensagem não enviada.");
                return;
            }

            _channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null!);
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null!, body: body);
        }
    }
}
