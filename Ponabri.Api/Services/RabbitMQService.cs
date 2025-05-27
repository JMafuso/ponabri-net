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
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMQProducer() // Idealmente, injetar configurações
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = "localhost" }; // Use sua config do RabbitMQ
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();
            }
            catch (Exception ex)
            {
                // Logar erro de conexão com RabbitMQ
                Console.WriteLine($"Erro ao conectar ao RabbitMQ: {ex.Message}");
                // Tratar ou relançar a exceção conforme necessário
                // Em um cenário real, você pode ter um fallback ou um sistema de retry.
                // Para este exemplo, se não conectar, as mensagens não serão enviadas.
                _connection = null; 
                _channel = null;
            }
        }

        public void SendMessage<T>(T message, string queueName)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                Console.WriteLine("Canal RabbitMQ não está disponível. Mensagem não enviada.");
                return; // Não envia se não houver canal
            }

            _channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
        }
    }
} 