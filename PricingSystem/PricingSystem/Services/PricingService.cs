using Grpc.Core;
using PricingSystem.Interfaces;
using System.Collections.Concurrent;
using System.Threading.Channels;
using PricingSystem.Protos;

namespace PricingSystem.Services
{
    public class PricingService : PricingServiceBase, IPricingService
    {
        private const int _checkRate = 30000;
        private readonly ILogger<PricingService> _logger;
        private readonly ILiveMarketDataCache _liveMarketDataCache;
        //These would be accessed from the database, but here I have hardcoded for testing
        private HashSet<string> _tickers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IBM", "AMZN", "AAPL" };
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(10);
        public HashSet<string> Tickers
        {
            get => _tickers;
            set { _tickers = value; }
        }
        private void RefreshTickers() 
        {
            //access db, get all current valid Tickers from the db
            var dbTickers = new List<string>();
            //example code for the refresh to add any new tickers to the collection to be checked
            //consider thread safety, while another thread is getting the tickers and this thread is overwriting tickers
            //change the type of the collection to something thread safe
            //keep this here as a template, but if using a db and actually refreshing these would be things to consider
            Tickers = dbTickers.ToHashSet();
        }
        public PricingService(ILogger<PricingService> logger, ILiveMarketDataCache liveMarketDataCache) 
            : base(_checkRate, logger)
        {
            _logger = logger;
            _liveMarketDataCache = liveMarketDataCache;
        }

        private bool ValidateTicker(string Ticker)
        {
            if (
                Ticker == null || 
                Ticker.Length > 5 || 
                Ticker.Length < 3 || 
                !Tickers.Contains(Ticker)
               )
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
                _logger.LogError($"Invalid or unsupported Ticker provided : {Ticker}");
                throw new ArgumentException($"Unsupported Ticker provided : {Ticker}");
            }
            else
            {
                var normalisedTicker = Tickers
                    .First(x => x.Equals(Ticker, StringComparison.OrdinalIgnoreCase));

                if (_liveMarketDataCache.GetPrices().TryGetValue(normalisedTicker, out var price))
                {
                    return price;
                }
                else
                {
                    _logger.LogWarning($"Price data not available for Ticker {normalisedTicker}");
                    throw new InvalidOperationException($"Current price not available for Ticker {normalisedTicker}");
                }
            }
        }
        public IList<string> GetTickers()
        {
            return Tickers.ToList();
        }

        public IDictionary<string, decimal> GetPrices()
        {
            return _liveMarketDataCache.GetPrices();
        }

        private async Task SetPrice(string Ticker)
        {
            if (string.IsNullOrEmpty(Ticker))
            {
                _logger.LogError("SetPrice called with null or empty Ticker.");
                return;
            }
            await _semaphoreSlim
                .WaitAsync()
                .ConfigureAwait(false);
            try
            {
                await _liveMarketDataCache.UpdateCacheAndNotifySubscribersAsync(Ticker).ConfigureAwait(false);
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
            //RefreshTickers(); // from the db if any changes have happened

            var tasks = Tickers
                .Select(async ticker => await SetPrice(ticker)
                .ConfigureAwait(false));

            await Task.WhenAll(tasks);

            return true;
        }
    }
}
