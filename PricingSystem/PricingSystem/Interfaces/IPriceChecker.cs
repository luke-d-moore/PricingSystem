using PricingSystem.Responses;
using Serilog;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace PricingSystem.Interfaces
{
    public interface IPriceChecker
    {
        public Task<decimal?> GetPriceFromTicker(string ticker);
    }
}
