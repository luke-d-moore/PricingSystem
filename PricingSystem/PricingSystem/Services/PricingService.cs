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
        private readonly HashSet<string> _tickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IBM", "AMZN", "AAPL" };
        private readonly IDictionary<string, decimal> _prices = new ConcurrentDictionary<string, decimal>();
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(10);
        private const decimal InvalidPrice = 0m;

        private HashSet<string> Tickers
            {
            get { return _tickers; }
            }
        public PricingService(ILogger<PricingService> logger, IPriceChecker priceChecker) 
            : base(_checkRate, logger)
        {
            _logger = logger;
            _priceChecker = priceChecker;

            foreach(var ticker in _tickers)
            {
                _prices.TryAdd(ticker, InvalidPrice);
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
            if (!ValidateTicker(Ticker))
            {
                throw new ArgumentException("Ticker cannot be null or empty.", nameof(Ticker));
            }

            if (Tickers.Contains(Ticker))
            {
                var normalisedTicker = Tickers.First(x => x.Equals(Ticker, StringComparison.OrdinalIgnoreCase));

                if (_prices.TryGetValue(normalisedTicker, out var price))
                {
                    return price;
                }
                else
                {
                    _logger.LogWarning($"Price date not available for Ticker {normalisedTicker}");
                    throw new InvalidOperationException($"Current price not available for Ticker {normalisedTicker}");
                }
            }
            else
            {
                _logger.LogError($"Invalid or unsupported Ticker provided : {Ticker}");
                throw new ArgumentException($"Unsupported Ticker provided : {Ticker}");
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
            if (string.IsNullOrEmpty(Ticker))
            {
                _logger.LogError("SetPrice called with null or empty Ticker.");
                return;
            }
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                var price = await _priceChecker.GetPriceFromTicker(Ticker).ConfigureAwait(false);
                if (price > InvalidPrice)
                {
                    _prices[Ticker] = price;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting price for Ticker : {Ticker}");
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
