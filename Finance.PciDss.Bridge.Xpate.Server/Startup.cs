using System;
using Finance.PciDss.Bridge.Xpate.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using ProtoBuf.Grpc.Server;
using SimpleTrading.BaseMetrics;
using SimpleTrading.ServiceStatusReporterConnector;

namespace Finance.PciDss.Bridge.Xpate.Server
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            SettingsModel settingsModel = SimpleTrading.SettingsReader.SettingsReader.ReadSettings<SettingsModel>();
            services.BindSettings(settingsModel);
            services.BindLogger(settingsModel);
            services.BindXpateHttpCLient();
            services.BindGrpcServices(settingsModel);
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddControllers();
            services.AddGrpc();
            services.AddCodeFirstGrpc(option =>
            {
                option.Interceptors.Add<ErrorLoggerInterceptor>();
                option.BindMetricsInterceptors();
            });            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.BindServicesTree(typeof(Startup).Assembly);
            app.BindIsAlive();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<XpateGrpcService>();
                endpoints.MapMetrics();
            });
        }
    }
}
