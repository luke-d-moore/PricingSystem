using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PricingSystem.Interfaces;
using PricingSystem.Responses;
using PricingSystem.Services;
using System.Net;
using System.Text.Json;

namespace PricingSystemTests
{
    public class LiveMarketDataCacheTests
    {
        private readonly Mock<ILogger<LiveMarketDataCache>> _priceLogger;
        private readonly Mock<IConfiguration> _configuration;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly LiveMarketDataCache _LiveMarketDataCache;
        public LiveMarketDataCacheTests()
        {
            _priceLogger = new Mock<ILogger<LiveMarketDataCache>>();
            _configuration = new Mock<IConfiguration>();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _LiveMarketDataCache = new LiveMarketDataCache(_priceLogger.Object, _httpClientFactory.Object.CreateClient());
        }

        private IHttpClientFactory SetupFactory(HttpResponseMessage httpResponseMessage, bool ShouldFail = false, bool ThrowsException = false)
        {
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            if (!ShouldFail)
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

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://baseurl.com/[token]") };

            var mockHttpClientFactory = new Mock<IHttpClientFactory>();

            mockHttpClientFactory.Setup(factory => factory.CreateClient(It.IsAny<string>()))
                                 .Returns(httpClient);
            return mockHttpClientFactory.Object;
        }
        [Fact]
        public async Task GetPriceFromTicker_GivenValidTicker_ReturnsPriceSuccessfully()
        {
            // Arrange
            var expectedPrice = 123.45m;
            var ticker = "IBM";

            var mockResponseContent = JsonSerializer.Serialize(new PriceCheckResponse { currentPrice = expectedPrice });
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var liveMarketDataCache = new LiveMarketDataCache(
                _priceLogger.Object,
                SetupFactory(httpResponse).CreateClient()
            );

            // Act
            await liveMarketDataCache.UpdateCacheAndNotifySubscribersAsync(ticker);
            var result = liveMarketDataCache.GetPrices()[ticker];

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
            var result = await Assert.ThrowsAsync<ArgumentException>(async () => await _LiveMarketDataCache.UpdateCacheAndNotifySubscribersAsync(invalidTicker));
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

            var liveMarketDataCache = new LiveMarketDataCache(
                _priceLogger.Object,
                SetupFactory(httpResponse, true).CreateClient()
            );
            // Act and Assert
            var result = await Assert.ThrowsAsync<HttpRequestException>(async () => await liveMarketDataCache.UpdateCacheAndNotifySubscribersAsync(ticker));
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

            var liveMarketDataCache = new LiveMarketDataCache(
                _priceLogger.Object,
                SetupFactory(httpResponse, true, true).CreateClient()
            );
            // Act and Assert
            var result = await Assert.ThrowsAsync<Exception>(async () => await liveMarketDataCache.UpdateCacheAndNotifySubscribersAsync(ticker));
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

            var liveMarketDataCache = new LiveMarketDataCache(
                _priceLogger.Object,
                SetupFactory(httpResponse).CreateClient()
            );

            // Act and Assert
            var result = await Assert.ThrowsAsync<JsonException>(async () => await liveMarketDataCache.UpdateCacheAndNotifySubscribersAsync(ticker));
        }

        [Fact]
        public async Task GetPriceFromTicker_ApiReturnsJsonWithoutPriceValue_ZeroItemsinGetPricesCollection()
        {
            // Arrange
            var ticker = "IBM";
            var mockResponseContent = JsonSerializer.Serialize(new PriceCheckResponse { currentPrice = null });
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseContent, System.Text.Encoding.UTF8, "application/json")
            };

            var liveMarketDataCache = new LiveMarketDataCache(
                _priceLogger.Object,
                SetupFactory(httpResponse).CreateClient()
            );

            // Act and Assert
            await liveMarketDataCache.UpdateCacheAndNotifySubscribersAsync(ticker);

            var result = liveMarketDataCache.GetPrices().Count;

            Assert.Equal(0, result);
        }
    }
}
