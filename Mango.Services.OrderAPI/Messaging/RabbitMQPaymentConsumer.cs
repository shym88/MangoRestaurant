using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Mango.Services.OrderAPI.Repository;
using Mango.Services.OrderAPI.Messages;
using System.Text;
using Newtonsoft.Json;

namespace Mango.Services.OrderAPI.Messaging
{
    public class RabbitMQPaymentConsumer : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;
        private readonly string _hostname;
        private readonly string _password;
        private readonly string _username;

        private const string ExchangeName = "PublishSubscribePaymentUpdate_Exchange";
        private readonly OrderRepository _orderRepository;
        string queueName = "";


        public RabbitMQPaymentConsumer(OrderRepository orderRepository)
        {
            _hostname = "localhost";
            _password = "guest";
            _username = "guest";

            _orderRepository = orderRepository;

            var factory = new ConnectionFactory
            {
                HostName = _hostname,
                UserName = _username,
                Password = _password
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout);
            queueName = _channel.QueueDeclare().QueueName;
            _channel.QueueBind(queueName, ExchangeName, "");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());

                UpdatePaymentResultMessage updatePaymentResultMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(content);

                HandleMessage(updatePaymentResultMessage).GetAwaiter().GetResult();

                _channel.BasicAck(ea.DeliveryTag, false);

            };

            _channel.BasicConsume(queueName, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(UpdatePaymentResultMessage updatePaymentResultMessage)
        {

            try
            {
                await _orderRepository.UpdateOrderPaymentStatus(updatePaymentResultMessage.OrderId, updatePaymentResultMessage.Status);

            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
