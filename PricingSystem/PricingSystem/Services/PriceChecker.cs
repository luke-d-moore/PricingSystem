using PricingSystem.Response;
using Serilog;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using PricingSystem.Interfaces;
using System.Globalization;

namespace PricingSystem.Services
{
    public class PriceChecker : IPriceChecker
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseURL;
        private readonly ILogger<PriceChecker> _logger;
        public PriceChecker(IConfiguration configuration, ILogger<PriceChecker> logger)
        {
            _configuration = configuration;
            _baseURL = _configuration["ThirdPartyPriceCheckURL"];
            _logger = logger;
        }
        public async Task<decimal> GetPriceFromTicker(string ticker)
        {
            try
            {
                HttpClient client = new HttpClient();

                _logger.LogInformation($"GetPriceFromTicker Request sent for Ticker {ticker}");
                using (HttpResponseMessage response = await client.GetAsync(_baseURL.Replace("[Ticker]", ticker)).ConfigureAwait(false))
                {
                    using (HttpContent content = response.Content)
                    {
                        var json = await content.ReadAsStringAsync().ConfigureAwait(false);
                        _logger.LogInformation($"GetPriceFromTicker Response received for Ticker {ticker}, response was : {json}");
                        var responseObject = JsonSerializer.Deserialize<PriceCheckResponse>(json);
                        decimal? currentPrice = responseObject?.currentPrice;
                        return currentPrice.HasValue ? currentPrice.Value : 0m;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPriceFromTicker Failed with the following exception message : " + ex.Message);
            }
            return 0m;
        }
    }
}
