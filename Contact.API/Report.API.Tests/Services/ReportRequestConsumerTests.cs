using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Report.API.Data;
using Report.API.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class ReportRequestConsumerTests
{
    [Fact]
    public async Task ExecuteAsync_StopsGracefully_OnCancellation()
    {
        var loggerMock = new Mock<ILogger<ReportRequestConsumer>>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();

        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        var dbContextMock = new Mock<AppDbContext>(new DbContextOptionsBuilder<AppDbContext>().Options);

        serviceProviderMock.Setup(sp => sp.GetService(typeof(AppDbContext))).Returns(dbContextMock.Object);
        serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        scopeFactoryMock.Setup(sf => sf.CreateScope()).Returns(serviceScopeMock.Object);

        var consumer = new ReportRequestConsumer(loggerMock.Object, scopeFactoryMock.Object);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await consumer.StartAsync(cts.Token);

    }
}
