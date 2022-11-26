using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Mango.Services.Email.Repository;
using Mango.Services.Email.Messages;

namespace Mango.Services.Email.Messaging
{
    public class AzureServiceBusConsumer : IAzureServiceBusConsumer
    {
         
        private readonly string serviceBusConnectionString;
        private readonly string subscriptionEmail;
        private readonly string orderUpdatePaymentResultTopic;

        private ServiceBusProcessor orderUpdatePaymentStatusProcessor;

        private readonly IConfiguration _configuration;
        private readonly EmailRepository _emailRepository;
        public AzureServiceBusConsumer(EmailRepository emailRepository, IConfiguration configuration)
        {
            _emailRepository = emailRepository;
            _configuration = configuration;

            serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");
            subscriptionEmail = _configuration.GetValue<string>("SubscriptionEmail");
            orderUpdatePaymentResultTopic = _configuration.GetValue<string>("OrderUpdatePaymentResultTopic");

            var client = new ServiceBusClient(serviceBusConnectionString);

            
            orderUpdatePaymentStatusProcessor = client.CreateProcessor(orderUpdatePaymentResultTopic, subscriptionEmail);
           
        }
        public async Task Start()
        {
           
            orderUpdatePaymentStatusProcessor.ProcessMessageAsync += OnOrderPaymentUpdateReceived;
            orderUpdatePaymentStatusProcessor.ProcessErrorAsync += ErrorHandler;
            await orderUpdatePaymentStatusProcessor.StartProcessingAsync();


        }
        public async Task Stop()
        {
            

            await orderUpdatePaymentStatusProcessor.StopProcessingAsync();
            await orderUpdatePaymentStatusProcessor.DisposeAsync();
        }
        Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }
       

        private async Task OnOrderPaymentUpdateReceived(ProcessMessageEventArgs args)
        {
            var message = args.Message;
            var body = Encoding.UTF8.GetString(message.Body);

            UpdatePaymentResultMessage objMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(body);


            try
            {
                await _emailRepository.SendAndLogEmail(objMessage);
                await args.CompleteMessageAsync(args.Message);

            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
