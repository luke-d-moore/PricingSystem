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
        private readonly Mock<IPriceChecker> _priceChecker;
        private readonly ILogger<PricingService> _priceLogger;

        public PricingServiceTests()
        {
            _priceLogger = new Mock<ILogger<PricingService>>().Object;
            _priceChecker = new Mock<IPriceChecker>();
            _pricingService = new PricingService(_priceLogger, _priceChecker.Object);
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
            var result = _pricingService.GetCurrentPrice("IBM");
            //Act and Assert
            Assert.IsType<decimal>(result);
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