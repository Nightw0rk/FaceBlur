
using FaceBlurShared.Services.Factory.Queue;
using FaceBlurShared.Services.Factory.Store;
using FaceBlurShared.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceBlur
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSingleton<IFaceBlurStore>(sp=> {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var dbConfig = configuration.GetSection("DB");
                var type = dbConfig.GetValue<String>("type");
                var conStr = dbConfig.GetValue<String>("connectionString");
                return FaceBlurStoreFactory.CreateInstance(type, conStr);
            });
            services.AddSingleton<IFaceBlurQueue>(sp=> {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var dbConfig = configuration.GetSection("Queue");
                var type = dbConfig.GetValue<String>("type");
                var conStr = dbConfig.GetValue<String>("connectionString");
                /// TODO move task name to config
                return FaceBlurQueueFactory.CreateInstance(type, conStr, "task");
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "FaceBlur", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FaceBlur v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
