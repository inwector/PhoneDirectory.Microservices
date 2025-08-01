using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Report.API.Data;
using Report.API.Entities;
using System.Text.Json;

namespace Report.API.Services
{
    public class KafkaReportConsumer : BackgroundService
    {
        private readonly ILogger<KafkaReportConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public KafkaReportConsumer(ILogger<KafkaReportConsumer> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _configuration["Kafka:BootstrapServers"],
                GroupId = "report-api-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe("report-requests");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    var message = result.Message.Value;

                    var payload = JsonSerializer.Deserialize<ReportRequestMessage>(message);
                    if (payload is null || string.IsNullOrWhiteSpace(payload.Location))
                    {
                        _logger.LogWarning("Invalid message received: {Message}", message);
                        continue;
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var report = new Report.API.Entities.Report
                    {
                        Id = Guid.NewGuid(),
                        RequestDate = DateTime.UtcNow,
                        Status = ReportStatus.Preparing,
                        Location = payload.Location
                    };

                    context.Reports.Add(report);
                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("Report created for location: {Location}", payload.Location);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error.");
                }
            }

            consumer.Close();
        }

        private class ReportRequestMessage
        {
            public string Location { get; set; } = null!;
        }
    }
}