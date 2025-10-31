using PricingSystem.Response;
using Serilog;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PricingSystem.Classes
{
    public static class PriceChecker
    {
        public static async Task<decimal> GetPriceFromTicker(string ticker, string url)
        {
            try
            {
                HttpClient client = new HttpClient();

                using (HttpResponseMessage response = await client.GetAsync(url.Replace("[Ticker]", ticker)))
                {
                    using (HttpContent content = response.Content)
                    {
                        var json = await content.ReadAsStringAsync();
                        var responseObject = JsonSerializer.Deserialize<PriceCheckResponse>(json);
                        decimal? currentPrice = (responseObject?.currentPrice);
                        return currentPrice.HasValue? currentPrice.Value : 0m;
                    }
                }
            }catch (Exception ex)
            {
                Log.Logger.Error(ex, "GetPriceFromTicker Failed with the following exception message" + ex.Message);
            }
            return 0m;
        }
    }
}
