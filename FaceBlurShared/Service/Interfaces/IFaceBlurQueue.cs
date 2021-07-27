using FaceBlurShared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceBlurShared.Services.Interfaces
{
    public interface IFaceBlurQueue
    {
        IModel Channel {get;}
        void Push(FaceBlurItem item);

        void PushMessage(string message, string routeKey);

        void BindQueue(string queueName, string routeKey);
        void Close();
        void Comsume(string queueName, AsyncEventingBasicConsumer consumer);
    }
}
