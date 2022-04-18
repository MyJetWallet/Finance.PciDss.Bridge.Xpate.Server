using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;

namespace Finance.PciDss.Bridge.Xpate.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 8080, o => o.Protocols =
                            HttpProtocols.Http1);
                        options.Listen(IPAddress.Any, 5965, o => o.Protocols =
                            HttpProtocols.Http2);
                    });
                    webBuilder.UseUrls("http://*:5965", "http://*:8080");
                    webBuilder.UseStartup<Startup>();
                });
    }
}
