using Newtonsoft.Json;
using ProductUser.Microservice.Data;
using ProductUser.Microservice.Model;
using ProductUser.Microservice.Service;
using ProductUser.Microservice.Utility;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace ProductUser.Microservice.BackgroundServices
{
    public class RabbitMQBackgroundConsumerService : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;
        private IServiceScopeFactory _serviceScopeFactory;
        private string _exchangeName = StaticConfigurationManager.AppSetting["RabbitMqSettings:ExchangeName"];
        private string _exchangeType = StaticConfigurationManager.AppSetting["RabbitMqSettings:ExchangeType"];
        private string _queueName = StaticConfigurationManager.AppSetting["RabbitMqSettings:QueueName"];

        public RabbitMQBackgroundConsumerService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            InitRabbitMQ();
        }

        private void InitRabbitMQ()
        {
            var RabbitMQServer = "";
            var RabbitMQUserName = "";
            var RabbitMQPassword = "";

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            {
                RabbitMQServer = Environment.GetEnvironmentVariable("RABBIT_MQ_SERVER");
                RabbitMQUserName = Environment.GetEnvironmentVariable("RABBIT_MQ_USERNAME");
                RabbitMQPassword = Environment.GetEnvironmentVariable("RABBIT_MQ_PASSWORD");

            }
            else
            {
                RabbitMQServer = StaticConfigurationManager.AppSetting["RabbitMQ:RabbitURL"];
                RabbitMQUserName = StaticConfigurationManager.AppSetting["RabbitMQ:Username"];
                RabbitMQPassword = StaticConfigurationManager.AppSetting["RabbitMQ:Password"];
            }

            var factory = new ConnectionFactory()   
            {
                HostName = RabbitMQServer,
                UserName = RabbitMQUserName,
                Password = RabbitMQPassword
            };

            //create connection
            _connection = factory.CreateConnection();
            //create channel
            _channel = _connection.CreateModel();

            //direct exchange details  like name and type of exchange 
            _channel.ExchangeDeclare(_exchangeName, _exchangeType);

            //Declare Queue with Name and a few property related to Queue like durabality of msg, auto delete and many more
            _channel.QueueDeclare(queue: _queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _channel.QueueBind(queue: _queueName,
                exchange: _exchangeName,
                routingKey: _exchangeType);

            _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (ch, ea) =>
            {
                //received message
                var content = Encoding.UTF8.GetString(ea.Body.ToArray());

                // acknowledge the received message
                _channel.BasicAck(ea.DeliveryTag, false);

                //Deserilized Message
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var objMessg = JsonConvert.DeserializeObject<ProductOfferDetail>(message);
                //stored offer details into the database
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var _userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                    _userService.AddProductAsync(objMessg).GetAwaiter();
                }
            };

            consumer.Shutdown += OnConsumerShutdown;
            consumer.Registered += OnConsumerRegistered;
            consumer.Unregistered += OnConsumerUnregistered;
            consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

            _channel.BasicConsume(_queueName, false, consumer);
            return Task.CompletedTask;
        }
        private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
        private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
        private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
        private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
