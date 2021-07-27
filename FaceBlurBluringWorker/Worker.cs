using Azure.Storage.Blobs;
using FaceBlurShared.Models;
using FaceBlurShared.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using ImageProcessorCore;
using System.Net;

namespace FaceBlurBluringWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IFaceBlurQueue queue;
        private readonly IFaceBlurStore store;
        private readonly BlobServiceClient azureStorage;
        private readonly BlobContainerClient azureContainer;
        private readonly string queueName;

        public Worker(ILogger<Worker> logger, IConfiguration configuration, IFaceBlurQueue queue, IFaceBlurStore store, BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            this.queue = queue;
            this.store = store;
            this.azureStorage = blobServiceClient;
            var containerName = configuration.GetSection("AzureBlob").GetValue<String>("containerName");
            azureContainer = azureStorage.GetBlobContainerClient(containerName);
            queueName = configuration.GetValue<String>("queueName");
        }

        private Stream BlurFaces(System.Drawing.Rectangle[] faceRects, Stream sourceImage)
        {
            if (faceRects.Length > 0)
            {
                MemoryStream output = new MemoryStream();
                var image = new Image<ImageProcessorCore.Color, uint>(sourceImage);
                // Blur every detected face
                foreach (var faceRect in faceRects)
                {
                    var rectangle = new ImageProcessorCore.Rectangle(
                        faceRect.Top,
                        faceRect.Left,
                        faceRect.Width,
                        faceRect.Height);
                    image = image.BoxBlur(20, rectangle);

                }
                image.SaveAsJpeg(output);
                return output;
            }
            return sourceImage;
        }

        private void Send(string json)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://prod-01.northeurope.logic.azure.com:443/workflows/2d550d21d7a5484b87d06e211689c3e8/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=I6Hb_DIDMTFLZCRb0t6CasNZUcm2UsbuPmPc9zKdj-g");
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(json);
                streamWriter.Flush();
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Console.WriteLine(httpResponse);
        }

        protected async Task onMessage(object sender, BasicDeliverEventArgs @event)
        {
            var consumer = sender as AsyncEventingBasicConsumer;
            var json = Encoding.UTF8.GetString(@event.Body.ToArray());
            var message = JsonSerializer.Deserialize<FaceBlurItemBlurringMessage>(json);
            _logger.LogInformation($"Processing faceblur item with Id: '{message.Id}'.");
            consumer.Model.BasicAck(@event.DeliveryTag, false);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var item = await store.Get(message.Id);
            try
            {
                var containerName = Path.GetFileName(new Uri(item.publicUrl).AbsolutePath);
                var container = azureContainer.GetBlobClient(containerName);
                using (var image = new MemoryStream())
                {
                    await container.DownloadToAsync(image);
                    var result = BlurFaces(message.faces.ToArray(), image);
                    await container.DeleteAsync();
                    result.Seek(0, SeekOrigin.Begin);
                    var bytes = new byte[result.Length];
                    result.Read(bytes, 0, (int)result.Length);
                    await container.UploadAsync(new BinaryData(bytes));
                    stopwatch.Stop();
                    item.status = FaceBlurItemStatusEnum.Done;
                    item.processTime += stopwatch.ElapsedMilliseconds;
                    await store.Update(item);
                    Send(JsonSerializer.Serialize(new ResultBlurResoponseModel()
                    {
                        status = item.status.ToString(),
                        jobId = item.Id,
                        proccessedTime = item.processTime,
                        resultUrl = item.publicUrl
                    }));
                }

            }
            catch (Exception he)
            {
                item.status = FaceBlurItemStatusEnum.Error;
                item.errorMessage = he.Message;
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
