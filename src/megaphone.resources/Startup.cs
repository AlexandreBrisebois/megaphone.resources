using Megaphone.Resources.Core.Services.Events;
using Megaphone.Resources.Core.Services.Storage;
using Megaphone.Resources.Services;
using Megaphone.Resources.Services.Events;
using Megaphone.Resources.Services.Storage;
using Megaphone.Standard.Time;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Diagnostics;

namespace Megaphone.Resources
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
            services.AddControllers().AddDapr();

            if (Debugger.IsAttached)
            {
                services.AddSingleton<IEventService, MockEventService>();
                //services.AddSingleton<IResourceStorageService, InMemoryResourceStorageService>();
                services.AddSingleton<IResourceStorageService>(new FileSystemResourceStorageService());
            }
            else
            {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("USE-VOLUME-STORAGE")))
                {
                    services.AddSingleton<IResourceStorageService, DaprResourceStorageService>();
                }
                else
                {
                    services.AddSingleton<IResourceStorageService>(new FileSystemResourceStorageService());
                }

                services.AddSingleton<IEventService, DaprEventService>();
            }

            services.AddSingleton<IClock, UtcClock>();

            services.AddSingleton<IResourceService,ResourceService>();
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Megaphone.Resources", Version = "v1" });
            });

            string key = Environment.GetEnvironmentVariable("INSTRUMENTATION_KEY");
            if (!string.IsNullOrEmpty(key))
                services.AddApplicationInsightsTelemetry(key);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Megaphone.Resources v1"));
            }

            app.UseRouting();

            app.UseCloudEvents();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSubscribeHandler();
                endpoints.MapControllers();
            });
        }
    }
}
