using Confluent.Kafka;
using HookTrigger.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HookTrigger.Worker
{
    public class KubernetesWorker : BackgroundService
    {
        private readonly ConsumerConfig _config;
        private readonly ILogger<KubernetesWorker> _logger;

        public KubernetesWorker(ILogger<KubernetesWorker> logger, ConsumerConfig config)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Worker service started.");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Worker service stopped.");
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumer = new ConsumerBuilder<Null, string>(_config).Build();
            var topic = "mihai";
            try
            {
                consumer.Subscribe(topic);
                _logger.LogInformation($"Subscribed to topic {topic}.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var cr = consumer.Consume(stoppingToken);
                    var message = JsonSerializer.Deserialize<DockerHubPayload>(cr.Message.Value);
                    _logger.LogInformation("Received following message from Kafka: {@message}", message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred.");
            }

            return Task.CompletedTask;
        }
    }
}