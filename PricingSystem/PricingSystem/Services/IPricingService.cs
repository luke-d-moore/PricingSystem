namespace PricingSystem.Services
{
    public interface IPricingService : IHostedService
    {
        public IList<string> GetTickers();
        public IDictionary<string, decimal> GetPrices();
        public Task<decimal> GetCurrentPrice(string Ticker);

    }
}