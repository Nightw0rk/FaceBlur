
using FaceBlurShared.Services.Factory.Queue.Implentation;
using FaceBlurShared.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceBlurShared.Services.Factory.Queue
{
    public class FaceBlurQueueFactory
    {
        public static IFaceBlurQueue CreateInstance(string type, string connectionString, string exchange)
        {
            switch (type)
            {
                case "RabbitMQ":
                    return new FaceBlurQueueRabbitMq(connectionString, exchange);
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
