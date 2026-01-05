namespace PricingSystem.Interfaces
{
    public interface ILiveMarketDataCache
    {
        public Task UpdateCacheAndNotifySubscribersAsync(string ticker);
        public IDictionary<string, decimal> GetPrices();
    }
}
