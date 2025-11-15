using PricingSystem.Responses;
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
        private IHttpClientFactory _clientFactory;
        private readonly HttpClient _client;
        public string BaseURL
        {
            get { return _baseURL; }
        }
        public PriceChecker(IConfiguration configuration, ILogger<PriceChecker> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _baseURL = _configuration["ThirdPartyPriceCheckURL"];
            _logger = logger;
            _clientFactory = httpClientFactory;
            _client = _clientFactory.CreateClient();
        }
        public async Task<decimal> GetPriceFromTicker(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                _logger.LogWarning("GetPriceFromTicker called with null or empty ticker.");
                throw new ArgumentException("Ticker cannot be null or empty.", nameof(ticker));
            }
            try
            {
                _logger.LogInformation($"GetPriceFromTicker Request sent for Ticker {ticker}");

                var requestUrl = GetRequestURL(ticker);

                using (HttpResponseMessage response = await _client.GetAsync(requestUrl).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    _logger.LogInformation($"GetPriceFromTicker Response received for Ticker {ticker}, response was: {json}");

                    var responseObject = JsonSerializer.Deserialize<PriceCheckResponse>(json);

                    var currentPrice = responseObject?.currentPrice;

                    if (!currentPrice.HasValue)
                    {
                        throw new InvalidOperationException($"Price data missing in valid response for ticker: {ticker}");
                    }

                    return currentPrice.Value;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP request failed for Ticker {ticker}. Status Code: {ex.StatusCode}");
                throw;
            }
            catch (JsonException ex) 
            {
                _logger.LogError(ex, $"Failed to deserialize price response for Ticker {ticker}");
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while fetching price for Ticker {ticker}.");
                throw;
            }
        }

        public string GetRequestURL(string ticker)
        {
            return BaseURL.Replace("[Ticker]", ticker);
        }
    }
}
