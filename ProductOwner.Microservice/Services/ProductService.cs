using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ProductOwner.Microservice.Configurations;
using ProductOwner.Microservice.Data;
using ProductOwner.Microservice.Model;
using ProductOwner.Microservice.Utility;
using RabbitMQ.Client;
using System.Text;

namespace ProductOwner.Microservice.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly RabbitMQConfiguration _rabbitMqConfiguration;

        public ProductService(ApplicationDbContext dbContext, IOptionsMonitor<RabbitMQConfiguration> rabbitMqConfiguration)
        {
            _dbContext = dbContext;
            _rabbitMqConfiguration = rabbitMqConfiguration.CurrentValue;
        }
        public async Task<ProductDetails> AddProductAsync(ProductDetails product)
        {
            var result = _dbContext.Products.Add(product);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch(Exception ex)
            {

            }
            return result.Entity;
        }

        public async Task<ProductDetails> GetProductByIdAsync(int id)
        {
            return await _dbContext.Products.FindAsync(id);
        }

        public async Task<IEnumerable<ProductDetails>> GetProductListAsync()
        {
            return await _dbContext.Products.ToListAsync();
        }

        public bool SendProductOffer(ProductOfferDetail productOfferDetails)
        {
            string exchangeName = StaticConfigurationManager.AppSetting["RabbitMqSettings:ExchangeName"];
            string exchangeType = StaticConfigurationManager.AppSetting["RabbitMqSettings:ExchhangeType"];
            string queueName = StaticConfigurationManager.AppSetting["RabbitMqSettings:QueueName"];
            string routerKey = StaticConfigurationManager.AppSetting["RabbitMqSettings:RouteKey"];

            #region Configurar variaveis

            string? RabbitMQServer;
            string? RabbitMQUserName;
            string? RabbitMQPassword;
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            {
                RabbitMQServer = Environment.GetEnvironmentVariable("RABBIT_MQ_SERVER");
                RabbitMQUserName = Environment.GetEnvironmentVariable("RABBIT_MQ_USERNAME");
                RabbitMQPassword = Environment.GetEnvironmentVariable("RABBIT_MQ_PASSWORD");

            }
            else
            {
                RabbitMQServer = _rabbitMqConfiguration.RabbitURL;
                RabbitMQUserName = _rabbitMqConfiguration.Username;
                RabbitMQPassword = _rabbitMqConfiguration.Password;
            }
            #endregion

            try
            {
                var factory = new ConnectionFactory()
                { HostName = RabbitMQServer, UserName = RabbitMQUserName, Password = RabbitMQPassword };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    //Direct Exchange Details like name and type of exchange
                    channel.ExchangeDeclare(exchangeName, exchangeType);

                    //Declare Queue with Name and a few property related to Queue like durabality of msg, auto delete and many more
                    channel.QueueDeclare(queue: queueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    //Bind Queue with Exhange and routing details
                    channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: routerKey);

                    //Seriliaze object using Newtonsoft library
                    string productDetail = JsonConvert.SerializeObject(productOfferDetails);
                    var body = Encoding.UTF8.GetBytes(productDetail);

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = true;

                    //publish msg
                    channel.BasicPublish(exchange: exchangeName,
                                         routingKey: routerKey,
                                         basicProperties: properties,
                                         body: body);

                    return true;
                }
            }

            catch (Exception ex)
            {
            }
            return false;
        }
    }
}
