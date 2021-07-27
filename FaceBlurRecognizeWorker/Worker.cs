
using FaceBlurShared.Models;
using FaceBlurShared.Services.Interfaces;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FaceBlurRecognizeWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IFaceBlurQueue queue;
        private readonly IFaceBlurStore store;
        private readonly IFaceClient faceClient;
        private readonly string queueName;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IFaceBlurQueue queue, IFaceBlurStore store, IFaceClient faceClient)
        {
            _logger = logger;
            this.queue = queue;
            this.store = store;
            this.faceClient = faceClient;

            queueName = configuration.GetValue<String>("queueName");
        }
        protected async Task onMessage(object sender, BasicDeliverEventArgs @event)
        {
            var consumer = sender as AsyncEventingBasicConsumer;
            var id = Encoding.UTF8.GetString(@event.Body.ToArray());
            _logger.LogInformation($"Processing faceblur item with Id: '{id}'.");
            consumer.Model.BasicAck(@event.DeliveryTag, false);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var item = await store.Get(id);
            try
            {
                var facesResults = await faceClient.Face.DetectWithUrlAsync(item.publicUrl, true, true, null, recognitionModel: "recognition_01", returnRecognitionModel: true);
                stopwatch.Stop();
                item.status = FaceBlurItemStatusEnum.Bluring;
                item.errorMessage = "";
                item.processTime += stopwatch.ElapsedMilliseconds;
                await store.Update(item);
                var message = new FaceBlurItemBlurringMessage() {
                    Id = item.Id,
                    faces = facesResults.Select(x=> new Rectangle() { 
                        X = x.FaceRectangle.Top, 
                        Y = x.FaceRectangle.Left, 
                        Width = x.FaceRectangle.Width, 
                        Height = x.FaceRectangle.Height 
                    }).ToList()
                };
                queue.PushMessage(JsonSerializer.Serialize(message), FaceBlurItemStatusEnum.Bluring.ToString());

            }
            catch (APIErrorException error)
            {
                item.status = FaceBlurItemStatusEnum.Error;
                item.errorMessage = error.Message;
                await store.Update(item);
            }

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var consumer = new AsyncEventingBasicConsumer(queue.Channel);
            consumer.Received += onMessage;
            queue.Comsume(queueName, consumer);
            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await base.StopAsync(cancellationToken);
            queue.Close();
        }
    }
}
