using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Account.Command.Infrastructure.Services;

public class KafkaTopicInitializer : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaTopicInitializer> _logger;
    private const int _replicationFactor = 1;
    private const int _numPartitions = 1;

    public KafkaTopicInitializer(IConfiguration configuration, ILogger<KafkaTopicInitializer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("kafka");
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Kafka connection string not found. Skipping topic initialization.");
            return;
        }

        var config = new AdminClientConfig { BootstrapServers = connectionString };

        using var adminClient = new AdminClientBuilder(config).Build();

        var topics = new[] { "funds-deposited", "funds-withdrawn" };

        try
        {
            var existingTopics = adminClient.GetMetadata(TimeSpan.FromSeconds(10)).Topics;
            
            var topicsToCreate = new List<TopicSpecification>();
            foreach (var topic in topics)
            {
                if (!existingTopics.Any(t => t.Topic == topic))
                {
                    topicsToCreate.Add(new TopicSpecification 
                    { 
                        Name = topic, 
                        NumPartitions = _numPartitions, 
                        ReplicationFactor = _replicationFactor 
                    });
                }
            }

            if (topicsToCreate.Any())
            {
                _logger.LogInformation("Creating Kafka topics: {Topics}", string.Join(", ", topicsToCreate.Select(t => t.Name)));
                await adminClient.CreateTopicsAsync(topicsToCreate);
            }
            else
            {
                _logger.LogInformation("All required Kafka topics already exist.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initializing Kafka topics.");
            // We don't throw here to avoid preventing app startup if Kafka is temporarily down, 
            // though consumers might fail subsequently.
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
