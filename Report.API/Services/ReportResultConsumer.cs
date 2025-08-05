using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Report.API.Data;
using Report.API.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Report.API.Services
{
    public class ReportResultConsumer : BackgroundService
    {
        private readonly ILogger<ReportResultConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public ReportResultConsumer(ILogger<ReportResultConsumer> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReportResultConsumer started...");

            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "report-result-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                SessionTimeoutMs = 6000,
                HeartbeatIntervalMs = 2000
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            try
            {
                consumer.Subscribe("report-results");
                _logger.LogInformation("Subscribed to 'report-results' topic, listening for messages...");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));

                        if (result != null)
                        {
                            var message = result.Message.Value;
                            _logger.LogInformation("Received message: {Message}", message);

                            try
                            {
                                var payload = JsonSerializer.Deserialize<ReportRequestMessage>(message);

                                if (payload is null || string.IsNullOrWhiteSpace(payload.Location))
                                {
                                    _logger.LogWarning("Invalid message received: {Message}", message);
                                    continue;
                                }

                                using var scope = _serviceProvider.CreateScope();
                                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                                var contactApiClient = scope.ServiceProvider.GetRequiredService<ContactApiClient>();

                                var contactData = await contactApiClient.RequestStatsAsync(payload.Location);

                                var report = await dbContext.Reports.FindAsync(payload.Id);
                                if (report == null)
                                {
                                    _logger.LogWarning("Report not found: {ReportId}", payload.Id);
                                    continue;
                                }

                                report.Status = ReportStatus.Completed;

                                var reportDetail = new ReportDetail
                                {
                                    Id = Guid.NewGuid(),
                                    Location = contactData.Location,
                                    PersonCount = contactData.PersonCount,
                                    PhoneNumberCount = contactData.PhoneNumberCount,
                                    ReportId = payload.Id,

                                };

                                dbContext.Add(reportDetail);

                                await dbContext.SaveChangesAsync(stoppingToken);

                                _logger.LogInformation("Report updated for location: {Location} with ID: {ReportId}",
                                    payload.Location, report.Id);

                                _logger.LogInformation("ReportDetail created for reportId: {ReportId} with ID: {ReportDetailId}",
                                    report.Id, reportDetail.Id);

                            }
                            catch (JsonException jsonEx)
                            {
                                _logger.LogError(jsonEx, "Failed to deserialize message: {Message}", message);
                            }
                            catch (Exception dbEx)
                            {
                                _logger.LogError(dbEx, "Database error while processing message: {Message}", message);
                            }
                        }
                    }
                    catch (ConsumeException consumeEx)
                    {
                        _logger.LogError(consumeEx, "Kafka consume error: {ErrorReason}", consumeEx.Error.Reason);
                    }

                    await Task.Delay(100, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in ReportResultConsumer");
            }
            finally
            {
                try
                {
                    consumer.Close();
                    _logger.LogInformation("ReportResultConsumer stopped gracefully");
                }
                catch (Exception closeEx)
                {
                    _logger.LogError(closeEx, "Error while closing Kafka consumer");
                }
            }
        }

        private class ReportRequestMessage
        {
            [JsonPropertyName("ReportId")]
            public Guid Id { get; set; }
            public string Location { get; set; } = null!;
            public int PersonCount { get; set; }
            public int PhoneNumberCount { get; set; }
        }
    }
}