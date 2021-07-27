using FaceBlurShared.Models;
using FaceBlurShared.Services.Factory.Queue;
using FaceBlurShared.Services.Factory.Store;
using FaceBlurShared.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

using Microsoft.Azure.CognitiveServices.Vision.Face;

namespace FaceBlurRecognizeWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IFaceClient>(sp =>
                    {
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        var faceConfig = configuration.GetSection("AzureFace");
                        var apiKy = faceConfig.GetValue<String>("apiKey");
                        var endpoint = faceConfig.GetValue<String>("endpoint");
                        return new FaceClient(
                            new ApiKeyServiceClientCredentials(apiKy),
                            new System.Net.Http.DelegatingHandler[] { }
                        )
                        {
                            Endpoint = endpoint
                        };
                    });

                    services.AddSingleton<IFaceBlurStore>(sp =>
                    {
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        var dbConfig = configuration.GetSection("DB");
                        var type = dbConfig.GetValue<String>("type");
                        var conStr = dbConfig.GetValue<String>("connectionString");
                        return FaceBlurStoreFactory.CreateInstance(type, conStr);
                    });
                    services.AddSingleton<IFaceBlurQueue>(sp =>
                    {
                        var configuration = sp.GetRequiredService<IConfiguration>();
                        var queueConfig = configuration.GetSection("Queue");
                        var type = queueConfig.GetValue<String>("type");
                        var conStr = queueConfig.GetValue<String>("connectionString");
                        /// TODO move task name to config
                        var queue = FaceBlurQueueFactory.CreateInstance(type, conStr, "task");
                        var queueName = configuration.GetValue<String>("queueName");
                        queue.BindQueue(queueName, FaceBlurItemStatusEnum.Recognizing.ToString());

                        return queue;
                    });
                    services.AddHostedService<Worker>();
                });
    }
}
