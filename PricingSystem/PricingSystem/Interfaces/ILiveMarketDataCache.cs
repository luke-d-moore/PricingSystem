namespace PricingSystem.Interfaces
{
    public interface ILiveMarketDataCache
    {
        public Task SetPriceFromTicker(string ticker);
        public IDictionary<string, decimal> GetPrices();
    }
}
