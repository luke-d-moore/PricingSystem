using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using PricingSystem.Interfaces;
using PricingSystem.Responses;
using PricingSystem.Services;
using Microsoft.Extensions.Configuration;
using System.Timers;

namespace PricingSystemTests
{
    public class PriceCheckerTests
    {
        private readonly Mock<ILogger<PriceChecker>> _priceLogger;
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly PriceChecker _priceChecker;
        public PriceCheckerTests()
        {
            _priceLogger = new Mock<ILogger<PriceChecker>>();
            _configuration = new Mock<IConfiguration>();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _configuration.SetupGet(x => x[It.Is<string>(s => s == "ThirdPartyPriceCheckURL")])
                                      .Returns("http://api.example.com/prices?ticker=[Ticker]");
            _priceChecker = new PriceChecker(_configuration.Object, _priceLogger.Object, _httpClientFactory.Object);
        }

        private IHttpClientFactory SetupFactory(HttpResponseMessage httpResponseMessage, bool ShouldFail = false, bool ThrowsException = false)
        {
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            if(!ShouldFail)
            {
                mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(httpResponseMessage);
            }
            else
            {
                mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Throws(ThrowsException ? new Exception() : new HttpRequestException());
            }

            var controlledHttpClient = new HttpClient(mockHandler.Object);

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();

            mockHttpClientFactory.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                                 .Returns(controlledHttpClient);
            return mockHttpClientFactory.Object;
        }
        [Fact]
        public async Task GetPriceFromTicker_GivenValidTicker_ReturnsPriceSuccessfully()
        {
            // Arrange
            var expectedPrice = 123.45m;
            var ticker = "TSLA";

            var mockResponseContent = JsonSerializer.Serialize(new PriceCheckResponse { currentPrice = expectedPrice });
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var priceChecker = new PriceChecker(
                _configuration.Object,
                _priceLogger.Object,
                SetupFactory(httpResponse)
            );

            // Act
            var result = await priceChecker.GetPriceFromTicker(ticker);

            // Assert
            result.Equals(expectedPrice);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetPriceFromTicker_GivenNullOrEmptyTicker_ThrowsArgumentException(string invalidTicker)
        {
            // Arrange
            // Act and Assert
            var result = await Assert.ThrowsAsync<ArgumentException>(async () => await _priceChecker.GetPriceFromTicker(invalidTicker));
        }

        [Fact]
        public async Task GetPriceFromTicker_ApiReturnsErrorStatusCode_ThrowsHttpRequestException()
        {
            // Arrange
            var ticker = "TSLA";
            var mockResponseContent = JsonSerializer.Serialize(new PriceCheckResponse { currentPrice = 0m });
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var priceChecker = new PriceChecker(
                _configuration.Object,
                _priceLogger.Object,
                SetupFactory(httpResponse, true)
            );
            // Act and Assert
            var result = await Assert.ThrowsAsync<HttpRequestException>(async () => await priceChecker.GetPriceFromTicker(ticker));
        }
        [Fact]
        public async Task GetPriceFromTicker_ApiReturnsErrorStatusCode_ThrowsException()
        {
            // Arrange
            var ticker = "TSLA";
            var mockResponseContent = JsonSerializer.Serialize(new PriceCheckResponse { currentPrice = 0m });
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var priceChecker = new PriceChecker(
                _configuration.Object,
                _priceLogger.Object,
                SetupFactory(httpResponse, true, true)
            );
            // Act and Assert
            var result = await Assert.ThrowsAsync<Exception>(async () => await priceChecker.GetPriceFromTicker(ticker));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task GetPriceFromTicker_ApiReturnsMalformedJson_ThrowsJsonException(string response)
        {
            // Arrange
            var ticker = "TSLA";
            var mockResponseContent = response;
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var priceChecker = new PriceChecker(
                _configuration.Object,
                _priceLogger.Object,
                SetupFactory(httpResponse)
            );

            // Act and Assert
            var result = await Assert.ThrowsAsync<JsonException>(async () => await priceChecker.GetPriceFromTicker(ticker));
        }

        [Fact]
        public async Task GetPriceFromTicker_ApiReturnsJsonWithoutPriceValue_ThrowsInvalidOperationException()
        {
            // Arrange
            var ticker = "TSLA";
            var mockResponseContent = JsonSerializer.Serialize(new PriceCheckResponse { currentPrice = null });
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var priceChecker = new PriceChecker(
                _configuration.Object,
                _priceLogger.Object,
                SetupFactory(httpResponse)
            );

            // Act and Assert
            var result = await Assert.ThrowsAsync<InvalidOperationException>(async () => await priceChecker.GetPriceFromTicker(ticker));
        }
    }
}
