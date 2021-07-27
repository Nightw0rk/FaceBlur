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
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FaceBlurDownloaderWorker
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

        public static async Task<byte[]> DownloadFile(string url)
        {
            using (var client = new HttpClient())
            {

                using (var result = await client.GetAsync(url))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return await result.Content.ReadAsByteArrayAsync();
                    }

                }
            }
            return null;
        }
        private string ComputeSha256Hash(byte[] rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(rawData);

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string GetImageFormat(byte[] buffer)
        {

            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            var jpeg = new byte[] { 255, 216, 255, 224 }; // jpeg
            var jpg = new byte[] { 255, 216, 255, 219 }; // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon

            if (bmp.SequenceEqual(buffer.Take(bmp.Length)))
                return "bmp";

            if (gif.SequenceEqual(buffer.Take(gif.Length)))
                return "gif";

            if (png.SequenceEqual(buffer.Take(png.Length)))
                return "png";

            if (tiff.SequenceEqual(buffer.Take(tiff.Length)))
                return "tiff";

            if (tiff2.SequenceEqual(buffer.Take(tiff2.Length)))
                return "tiff";

            if (jpeg.SequenceEqual(buffer.Take(jpeg.Length)))
                return "jpeg";

            if (jpeg2.SequenceEqual(buffer.Take(jpeg2.Length)))
                return "jpeg";
            if (jpg.SequenceEqual(buffer.Take(jpg.Length)))
                return "jpg";

            return String.Empty;
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
                if (item != null)
                {
                    item.status = FaceBlurItemStatusEnum.Downloading;
                    await store.Update(item);
                    var data = await DownloadFile(item.originUrl);
                    var extension = GetImageFormat(data);
                    if (String.IsNullOrEmpty(extension))
                    {
                        item.status = FaceBlurItemStatusEnum.Error;
                        item.errorMessage = "It is not image";
                        await store.Update(item);
                        return;
                    }
                    var hash = ComputeSha256Hash(data);
                    var dublicate = await store.GetByHash(hash);
                    if (dublicate != null)
                    {
                        stopwatch.Stop();
                        item.hash = hash;
                        item.status = FaceBlurItemStatusEnum.Done;
                        item.processTime = stopwatch.ElapsedMilliseconds;
                        item.publicUrl = dublicate.publicUrl;
                        item.errorMessage = "";
                        await store.Update(item);
                    }
                    else
                    {
                        var blobClient = azureContainer.GetBlobClient($"{item.Id}.{extension}");
                        var blobExists = await blobClient.ExistsAsync();
                        if (!blobExists)
                            await blobClient.UploadAsync(new BinaryData(data));
                        stopwatch.Stop();
                        item.hash = hash;
                        item.status = FaceBlurItemStatusEnum.Recognizing;
                        item.processTime = stopwatch.ElapsedMilliseconds;
                        item.errorMessage = "";
                        item.publicUrl = $"{azureContainer.Uri}/{item.Id}.{extension}";
                        await store.Update(item);
                        queue.Push(item);

                    }
                }
            }
            catch (HttpRequestException he)
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
