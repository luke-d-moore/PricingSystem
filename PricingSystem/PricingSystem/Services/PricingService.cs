using System.Collections.Concurrent;
using System.Threading;
using System.Timers;
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
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(10);
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

        private bool ValidateTicker(string Ticker)
        {
            if (Ticker == null || (Ticker.Length > 5 || Ticker.Length < 3))
            {
                _logger.LogError($"Ticker was invalid.");
                return false;
            }

            return true;
        }
        public decimal GetCurrentPrice(string Ticker)
        {
            if(!ValidateTicker(Ticker)) throw new ArgumentException("Invalid Ticker", "ticker");
            var allowedTickers = GetTickers().ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (allowedTickers.Contains(Ticker))
            {
                Ticker = allowedTickers.First(x => x.Equals(Ticker, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                _logger.LogError($"Ticker was invalid. Ticker was : {Ticker}");
                throw new ArgumentException("Invalid Ticker", "ticker");
            }
            if (_prices.TryGetValue(Ticker, out var price))
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

        private async Task SetPrice(string Ticker)
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                var price = await _priceChecker.GetPriceFromTicker(Ticker).ConfigureAwait(false);
                if (price > 0m)
                {
                    _prices[Ticker] = price;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, $"The Ticker was {Ticker}");
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        protected async override Task<bool> SetCurrentPrices()
        {
            var tasks = _tickers.Select(async ticker => await SetPrice(ticker).ConfigureAwait(false));

            await Task.WhenAll(tasks);

            return true;
        }
    }
}
