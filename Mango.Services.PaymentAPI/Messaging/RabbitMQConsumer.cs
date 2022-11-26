using Mango.MessageBus;
using Mango.Services.PaymentAPI.Messages;
using Mango.Services.PaymentAPI.RabbitMQSender;
using Microsoft.AspNetCore.Connections;
using Newtonsoft.Json;
using PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace Mango.Services.PaymentAPI.Messaging
{
    public class RabbitMQConsumer : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;
        private readonly string _hostname;
        private readonly string _password;
        private readonly string _username;
        private readonly IProcessPayment _processPayment;

        private readonly IRabbitMQPaymentMessageSender _rabbitMQPaymentMessageSender;

        public RabbitMQConsumer(IRabbitMQPaymentMessageSender rabbitMQOrderMessageSender, IProcessPayment processPayment)
        {
            _hostname = "localhost";
            _password = "guest";
            _username = "guest";

            _processPayment = processPayment;
            _rabbitMQPaymentMessageSender = rabbitMQOrderMessageSender;

            var factory = new ConnectionFactory
            {
                HostName = _hostname,
                UserName = _username,
                Password = _password
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "orderpaymentprocesstopic", false, false, false, arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());

                PaymentRequestMessage paymentRequestMessage = JsonConvert.DeserializeObject<PaymentRequestMessage>(content);

                HandleMessage(paymentRequestMessage).GetAwaiter().GetResult();

                _channel.BasicAck(ea.DeliveryTag, false);

            };

            _channel.BasicConsume("orderpaymentprocesstopic", false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(PaymentRequestMessage paymentRequestMessage)
        {


            var result = _processPayment.PaymentProcessor();

            UpdatePaymentResultMessage updatePaymentResultMessage = new()
            {
                Status = result,
                OrderId = paymentRequestMessage.OrderId,
                Email = paymentRequestMessage.Email,
            };

            try
            {
                _rabbitMQPaymentMessageSender.SendMessage(updatePaymentResultMessage, "");
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
