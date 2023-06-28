using System;
using System.Reflection.PortableExecutable;
using RabbitMQ.Client;

namespace Brer.Core
{
    public class BrerOptionsBuilder
    {
        public const string defaultLogin = "guest";
        public const int defaultPort = 5672;
        public const string localHost = "localhost";
        private string Host { get; set; }
        private int Port { get; set; }

        private string ExchangeName { get; set; }
        private string QueueName { get; set; }
        
        private string RabbitMQUser { get; set; }
        private string RabbitMQPass { get; set; }
        
        public BrerOptionsBuilder() {}

        public BrerOptionsBuilder WithAddress(string host, int port)
        {
            Host = host;
            Port = port;
            return this;
        }

        public BrerOptionsBuilder WithExchange(string exchange)
        {
            ExchangeName = exchange;
            return this;
        }

        public BrerOptionsBuilder WithQueueName(string queue)
        {
            QueueName = queue;
            return this;
        }

        public BrerOptionsBuilder WithUserName(string username)
        {
            RabbitMQUser = username;
            return this;
        }
        
        public BrerOptionsBuilder WithPassWord(string password)
        {
            RabbitMQPass = password;
            return this;
        }

        public BrerOptionsBuilder ReadFromEnviromentVariables()
        {
            Host = Environment.GetEnvironmentVariable("BrerHostName") ??  throw new ArgumentNullException("BrerHostName");
            Port = Convert.ToInt32(Environment.GetEnvironmentVariable("BrerPort") ??  throw new ArgumentNullException("BrerPort"));
            ExchangeName = Environment.GetEnvironmentVariable("BrerExchangeName") ??  throw new ArgumentNullException("BrerExchangeName");
            QueueName = Environment.GetEnvironmentVariable("BrerQueueName") ??  throw new ArgumentNullException("BrerQueueName");
            RabbitMQUser = Environment.GetEnvironmentVariable("BrerUserName") ??  throw new ArgumentNullException("BrerUserName");
            RabbitMQPass = Environment.GetEnvironmentVariable("BrerPassword") ??  throw new ArgumentNullException("BrerPassword");
            return this;
        }

        public BrerOptions Build()
        {
            return new BrerOptions(new ConnectionFactory
                {
                    Port = Port, 
                    HostName = Host, 
                    UserName = RabbitMQUser, 
                    Password = RabbitMQPass
                    
                }, 
                ExchangeName,
                QueueName);
        }
    }
}