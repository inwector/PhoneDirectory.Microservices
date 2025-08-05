using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Report.API.Data;
using Report.API.Entities;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Report.API.Services
{
    public class ReportRequestConsumer : BackgroundService
    {
        private readonly ILogger<ReportRequestConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ReportRequestConsumer(ILogger<ReportRequestConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ReportRequestConsumer started...");

            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "report-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                SessionTimeoutMs = 6000,
                HeartbeatIntervalMs = 2000
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            try
            {
                consumer.Subscribe("report-requests");
                _logger.LogInformation("Subscribed to 'report-requests' topic, listening for messages...");

                var producerConfig = new ProducerConfig { BootstrapServers = "localhost:9092" };
                using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));

                        if (consumeResult != null)
                        {
                            var location = consumeResult.Message.Value;
                            _logger.LogInformation("Processing report request for location: {Location}", location);

                            if (string.IsNullOrWhiteSpace(location))
                            {
                                _logger.LogWarning("Empty or null location received, skipping...");
                                continue;
                            }

                            try
                            {
                                using var scope = _scopeFactory.CreateScope();
                                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                                using var transaction = await dbContext.Database.BeginTransactionAsync(stoppingToken);

                                try
                                {
                                    var report = new Report.API.Entities.Report
                                    {
                                        Id = Guid.NewGuid(),
                                        RequestDate = DateTime.UtcNow,
                                        Status = ReportStatus.Preparing,
                                        Location = location,
                                    };

                                    dbContext.Reports.Add(report);
                                    await dbContext.SaveChangesAsync(stoppingToken);
                                    await transaction.CommitAsync(stoppingToken);

                                    _logger.LogInformation("Report created with ID: {ReportId}", report.Id);

                                    var resultMessage = new ReportProcessingMessage
                                    {
                                        ReportId = report.Id,
                                        Location = location
                                    };

                                    var messageJson = JsonSerializer.Serialize(resultMessage);
                                    await producer.ProduceAsync("report-results",
                                        new Message<Null, string> { Value = messageJson });

                                    _logger.LogInformation("Report processing message sent for ReportId: {ReportId}", report.Id);

                                }
                                catch (Exception dbEx)
                                {
                                    await transaction.RollbackAsync(stoppingToken);
                                    _logger.LogError(dbEx, "Database transaction failed for location: {Location}", location);
                                    throw;
                                }
                            }
                            catch (Exception processEx)
                            {
                                _logger.LogError(processEx, "Failed to process report request for location: {Location}", location);
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
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ReportRequestConsumer is stopping due to cancellation.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in ReportRequestConsumer");
            }
            finally
            {
                try
                {
                    consumer.Close();
                    _logger.LogInformation("ReportRequestConsumer stopped gracefully");
                }
                catch (Exception closeEx)
                {
                    _logger.LogError(closeEx, "Error while closing Kafka consumer");
                }
            }
        }
    }

    public class ReportProcessingMessage
    {
        public Guid ReportId { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}