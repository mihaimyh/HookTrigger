﻿using Confluent.Kafka;
using HookTrigger.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace HookTrigger.Api.Services
{
    public class KafkaProducer : IMessageSenderService<DockerHubPayload>
    {
        private readonly ILogger<KafkaProducer> _logger;
        private readonly ProducerConfig _producerConfig;

        public KafkaProducer(ILogger<KafkaProducer> logger, ProducerConfig producerConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _producerConfig = producerConfig ?? throw new ArgumentNullException(nameof(producerConfig));
        }

        public async Task SendMessageAsync(DockerHubPayload payload)
        {
            var serializedDockerHubPayload = JsonSerializer.Serialize(payload);

            // Create a producer that can be used to send messages to Kafka that have no key and a value of type string
            using var producer = new ProducerBuilder<Null, string>(_producerConfig).Build();

            // Construct the message to send (generic type must match what was used above when creating the producer)
            var message = new Message<Null, string>
            {
                Value = serializedDockerHubPayload
            };
            try
            {
                // Send the message to our test topic in Kafka
                var dr = await producer.ProduceAsync("mihai", message);
                _logger.LogInformation($"Produced message '{dr.Value}' to topic {dr.Topic}, partition {dr.Partition}, offset {dr.Offset}");

                producer.Flush(TimeSpan.FromSeconds(10));
            }
            catch (ProduceException<Null, string> ex)
            {
                _logger.LogError(ex, "An error occurred while producing a message.");
            }
        }
    }
}