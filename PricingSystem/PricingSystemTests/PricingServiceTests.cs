using PricingSystem;
using PricingSystem.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using PricingSystem.Interfaces;

namespace PricingSystemTests
{
    public class PricingServiceTests
    {
        private readonly IPricingService _pricingService;
        private readonly Mock<ILiveMarketDataCache> _liveMarketDataCache;
        private readonly ILogger<PricingService> _priceLogger;

        public PricingServiceTests()
        {
            _priceLogger = new Mock<ILogger<PricingService>>().Object;
            _liveMarketDataCache = new Mock<ILiveMarketDataCache>();
            _liveMarketDataCache.Setup(x => x.GetPrices()).Returns(new Dictionary<string, decimal>() { { "IBM", 100m } });
            _pricingService = new PricingService(_priceLogger, _liveMarketDataCache.Object);
        }
        public static IEnumerable<object[]> TickerData =>
        new List<object[]>
        {
                new object[] { null },
                new object[] { "wrong"},
                new object[] { "" }
        };

        [Fact]
        public async Task GetCurrentPrice_ValidTicker_ReturnsDecimalAsync()
        {
            //Arrange
            var pricingService = (PricingService)_pricingService;
            var result = _pricingService.GetCurrentPrice("IBM");
            //Act and Assert
            Assert.Equal(100m, result);
        }
        [Theory, MemberData(nameof(TickerData))]
        public void GetCurrentPrice_InValidTicker_ThrowsArgumentException(string ticker)
        {
            // Arrange
            // Act and Assert
            Assert.Throws<ArgumentException>(() => _pricingService.GetCurrentPrice(ticker));
        }
    }
}