namespace PricingSystem.Interfaces
{
    public interface ILiveMarketDataCache
    {
        public Task GetPriceFromTicker(string ticker);
        public IDictionary<string, decimal> GetPrices();
    }
}
