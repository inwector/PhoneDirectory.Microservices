using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Report.API.Services;
using Report.API.DTOs;

public class ContactApiClientTests
{
    private HttpClient GetMockHttpClient(HttpResponseMessage responseMessage)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(responseMessage)
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test.com")
        };
        return httpClient;
    }

    [Fact]
    public async Task GetAllPersonsAsync_ReturnsSuccessResponse()
    {
        var expectedContent = "[{\"id\":\"some-guid\",\"firstName\":\"John\"}]";
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedContent)
        };
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var response = await apiClient.GetAllPersonsAsync();

        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public async Task GetPersonAsync_ReturnsSuccessResponse()
    {
        var expectedContent = "{\"id\":\"some-guid\",\"firstName\":\"Jane\"}";
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(expectedContent)
        };
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var response = await apiClient.GetPersonAsync(Guid.NewGuid());

        Assert.True(response.IsSuccessStatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, content);
    }

    [Fact]
    public async Task CreatePersonAsync_ReturnsSuccessResponse()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.Created);
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var dto = new PersonCreateDto
        {
            FirstName = "Test",
            LastName = "User",
            Company = "TestCo",
            ContactInfos = new System.Collections.Generic.List<ContactInfoCreateDto>()
        };

        var response = await apiClient.CreatePersonAsync(dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeletePersonAsync_ReturnsSuccessResponse()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var response = await apiClient.DeletePersonAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddContactInfoAsync_ReturnsSuccessResponse()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.Created);
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var dto = new ContactInfoCreateDto
        {
            Content = "test@example.com",
            Type = "EmailAddress"
        };

        var response = await apiClient.AddContactInfoAsync(Guid.NewGuid(), dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteContactInfoAsync_ReturnsSuccessResponse()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.NoContent);
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var response = await apiClient.DeleteContactInfoAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RequestReportAsync_ReturnsSuccessResponse()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var response = await apiClient.RequestReportAsync(Guid.NewGuid());

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task RequestStatsAsync_ReturnsDeserializedObject_WhenResponseIsSuccess()
    {
        var jsonResponse = "{\"location\":\"Istanbul\",\"personCount\":10,\"phoneNumberCount\":20}";
        var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse)
        };
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var result = await apiClient.RequestStatsAsync("Istanbul");

        Assert.NotNull(result);
        Assert.Equal("Istanbul", result.Location);
        Assert.Equal(10, result.PersonCount);
        Assert.Equal(20, result.PhoneNumberCount);
    }

    [Fact]
    public async Task RequestStatsAsync_ReturnsNull_WhenResponseIsFailure()
    {
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var httpClient = GetMockHttpClient(responseMessage);
        var apiClient = new ContactApiClient(httpClient);

        var result = await apiClient.RequestStatsAsync("UnknownLocation");

        Assert.Null(result);
    }
}
