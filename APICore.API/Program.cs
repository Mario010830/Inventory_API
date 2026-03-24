using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace APICore.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Npgsql 6+: por defecto timestamptz solo acepta UTC. Activa compatibilidad con DateTime Local/Unspecified.
            // Preferible seguir usando DateTime.UtcNow en modelo/EF; este switch evita fallos si queda código con hora local.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
                    webBuilder.UseUrls($"http://0.0.0.0:{port}");
                });
    }
}