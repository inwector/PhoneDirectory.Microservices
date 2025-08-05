using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Report.API.Controllers;
using Report.API.Data;
using Report.API.Entities;
using System;
using System.Threading.Tasks;
using Xunit;

public class ReportsControllerTests
{
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RequestReport_ReturnsBadRequest_WhenLocationIsNullOrEmpty()
    {
        var context = GetDbContext();
        var loggerMock = new Mock<ILogger<ReportsController>>();
        var controller = new ReportsController(context, loggerMock.Object);

        var request = new CreateReportRequest { Location = "" };

        var result = await controller.RequestReport(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Location is required", badRequestResult.Value);
    }
}