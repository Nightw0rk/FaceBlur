using FaceBlurShared.Models;
using FaceBlurShared.Services.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceBlurShared.Services.Factory.Queue.Implentation
{
    public class FaceBlurQueueRabbitMq : IFaceBlurQueue
    {
        private IConnection _connection;
        private IModel _channel;
        private string _exchangeName;

        public FaceBlurQueueRabbitMq(string connectionString, string exchange)
        {
            var factory = new ConnectionFactory() { Uri = new Uri(connectionString), DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();

            _channel = _connection.CreateModel();
            _exchangeName = exchange;
            _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, true);
        }

        public IModel Channel { get => _channel; }

        public void BindQueue(string queueName, string routeKey)
        {
            _channel.QueueDeclare(queueName, true, false);
            _channel.QueueBind(queueName, _exchangeName, routeKey);
        }

        public void Close()
        {
            _connection.Close();
        }

        public void Comsume(string queueName, AsyncEventingBasicConsumer consumer)
        {
            _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        }

        public void Push(FaceBlurItem item)
        {
            PushMessage(item.Id, item.status.ToString());
        }

        public void PushMessage(string message, string routeKey)
        {
            _channel.BasicPublish(_exchangeName, routeKey, null, Encoding.UTF8.GetBytes(message));
        }
    }
}
