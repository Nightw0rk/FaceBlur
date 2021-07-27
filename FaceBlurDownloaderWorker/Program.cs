using FaceBlurShared.Models;
using FaceBlurShared.Services.Factory.Queue;
using FaceBlurShared.Services.Factory.Store;
using FaceBlurShared.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.Extensions.Azure;

namespace FaceBlurDownloaderWorker
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
                    services.AddAzureClients(builder =>
                    {
                        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                        var dbConfig = configuration.GetSection("AzureBlob");
                        var conStr = dbConfig.GetValue<String>("connectionString");
                        builder.AddBlobServiceClient(conStr);
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
                        var dbConfig = configuration.GetSection("Queue");
                        var type = dbConfig.GetValue<String>("type");
                        var conStr = dbConfig.GetValue<String>("connectionString");
                        /// TODO move task name to config
                        var queue = FaceBlurQueueFactory.CreateInstance(type, conStr, "task");
                        var queueName = configuration.GetValue<String>("queueName");
                        queue.BindQueue(queueName, FaceBlurItemStatusEnum.Idly.ToString());

                        return queue;
                    });
                    services.AddHostedService<Worker>();
                });
    }
}
