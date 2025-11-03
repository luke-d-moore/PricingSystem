namespace PricingSystem.Interfaces
{
    public interface IPricingService : IHostedService
    {
        public IList<string> GetTickers();
        public IDictionary<string, decimal> GetPrices();
        public Task<decimal> GetCurrentPrice(string Ticker);

    }
}