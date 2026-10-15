using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BitWrite.OcelotConsole.SDK;
using BitWrite.OcelotConsole.SDK.Clients;
using BitWrite.OcelotConsole.SDK.Models.Requests;
using BitWrite.OcelotConsole.SDK.Models.Responses;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Xunit;

namespace BitWrite.OcelotConsole.SDK.Tests.Clients;

public class RoutesClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<OcelotConsoleClient>> _mockLogger;
    private readonly OcelotConsoleClientOptions _options;
    private readonly OcelotConsoleClient _client;
    private readonly RoutesClient _routesClient;

    public RoutesClientTests()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(new Mock<HttpMessageHandler>().Object);
        _mockLogger = new Mock<ILogger<OcelotConsoleClient>>();
        _options = new OcelotConsoleClientOptions { BaseAddress = "https://api.example.com" };

        _client = new OcelotConsoleClient(
            new HttpClient(new Mock<HttpMessageHandler>().Object),
            new JwtTokenProvider(new HttpClient(new Mock<HttpMessageHandler>().Object), Mock.Of<ILogger<JwtTokenProvider>>(), new JwtTokenOptions { BaseAddress = "https://api.example.com" }),
            Mock.Of<ILogger<OcelotConsoleClient>>(),
            _options);

        _routesClient = new RoutesClient(_client);
    }

    [Fact]
    public void Constructor_ShouldCreateClient()
    {
        // Act & Assert
        _routesClient.Should().NotBeNull();
    }
}
