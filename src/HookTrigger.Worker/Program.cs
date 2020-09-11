using Confluent.Kafka;
using HookTrigger.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;

namespace HookTrigger.Worker
{
    public class Program
    {
        private static IConfiguration Configuration { get; set; }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var config = new ConsumerConfig();
                    Configuration.Bind("Consumer", config);
                    services.AddSingleton(config);
                    services.AddSingleton<IKubernetesService, KubernetesService>();
                    services.AddHostedService<KubernetesWorker>();
                }).UseSerilog();

        public static void Main(string[] args)
        {
            try
            {
                Configuration = LoadConfiguration();
                Log.Logger = new LoggerConfiguration()
                        .ReadFrom.Configuration(Configuration)
                        .CreateLogger();
                Log.Logger.Information("Starting...");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An error occurred.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IConfiguration LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("serilog.json", optional: true, reloadOnChange: true)
             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
             .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
             .AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true)
             .AddEnvironmentVariables();

            return builder.Build();
        }
    }
}