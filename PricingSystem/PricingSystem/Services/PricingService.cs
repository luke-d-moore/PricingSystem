using System.Collections.Concurrent;
using System.Threading;
using PricingSystem.Interfaces;

namespace PricingSystem.Services
{
    public class PricingService : PricingServiceBase, IPricingService
    {
        private const int _checkRate = 30000;
        private readonly ILogger<PricingService> _logger;
        private readonly IPriceChecker _priceChecker;
        //These would be accessed from the database, but here I have hardcoded for testing
        private readonly HashSet<string> _tickers = new HashSet<string>() { "IBM", "AMZN", "AAPL" };
        private readonly IDictionary<string, decimal> _prices = new ConcurrentDictionary<string, decimal>();
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(10,20);
        public PricingService(ILogger<PricingService> logger, IPriceChecker priceChecker) 
            : base(_checkRate, logger)
        {
            _logger = logger;
            _priceChecker = priceChecker;

            foreach(var ticker in _tickers)
            {
                _prices.TryAdd(ticker, 0m);
            }
        }
        public async Task<decimal> GetCurrentPrice(string Ticker)
        {
            await Task.Delay(0);
            if(_prices.TryGetValue(Ticker, out var price))
            {
                return price;
            }
            else
            {
                throw new ArgumentException("Invalid Ticker", "ticker");
            }

        }
        public IList<string> GetTickers()
        {
            return _tickers.ToList();
        }

        public IDictionary<string, decimal> GetPrices()
        {
            return _prices;
        }

        protected async override Task<bool> SetCurrentPrices()
        {
            var tasks = _tickers.Select(async ticker =>
                {
                    await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var price = await _priceChecker.GetPriceFromTicker(ticker).ConfigureAwait(false);
                        if (price > 0m)
                        {
                            _prices[ticker] = price;
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.LogError(ex.Message, $"The Ticker was {ticker}");
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }
                }
            );

            await Task.WhenAll(tasks);

            return true;
        }
    }
}
