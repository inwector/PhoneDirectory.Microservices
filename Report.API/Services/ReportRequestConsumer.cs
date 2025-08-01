using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Report.API.Data;
using Report.API.Entities;
using System.Text.Json;

namespace Report.API.Services
{
    public class ReportRequestConsumer : BackgroundService
    {
        private readonly ILogger<ReportRequestConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly string _topic = "report-requests";

        public ReportRequestConsumer(ILogger<ReportRequestConsumer> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                GroupId = "report-service-group",
                BootstrapServers = "localhost:9092",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    _logger.LogInformation($"Received report request: {consumeResult.Message.Value}");

                    var location = consumeResult.Message.Value;

                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var report = new Report.API.Entities.Report
                    {
                        Id = Guid.NewGuid(),
                        RequestDate = DateTime.UtcNow,
                        Status = ReportStatus.Preparing
                    };

                    dbContext.Reports.Add(report);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    var reportDetail = new ReportDetail
                    {
                        Id = Guid.NewGuid(),
                        Location = location,
                        PersonCount = 10,       // TODO: gerçek veriyi buraya koy
                        PhoneNumberCount = 25,  // TODO: gerçek veriyi buraya koy
                        ReportId = report.Id
                    };

                    dbContext.ReportDetails.Add(reportDetail);

                    report.Status = ReportStatus.Completed;

                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation($"Report completed for location: {location}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Kafka consumer stopping.");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}