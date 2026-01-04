using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using PricingSystem.Interfaces;
using PricingSystem.Protos;
using PricingSystem.Responses;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;

namespace PricingSystem.Services
{
    public class LiveMarketDataCache : GrpcPricingService.GrpcPricingServiceBase, ILiveMarketDataCache
    {
        private readonly ILogger<LiveMarketDataCache> _logger;
        private readonly HttpClient _client;
        public readonly ConcurrentBag<Channel<PriceUpdate>> _subscribers = new();
        private readonly ConcurrentDictionary<string, decimal> _prices = new ConcurrentDictionary<string, decimal>();
        public LiveMarketDataCache(ILogger<LiveMarketDataCache> logger, HttpClient httpClient)
        {
            _logger = logger;
            _client = httpClient;
        }
        public async Task GetPriceFromTicker(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                _logger.LogWarning("GetPriceFromTicker called with null or empty ticker.");
                throw new ArgumentException("Ticker cannot be null or empty.", nameof(ticker));
            }
            try
            {
                _logger.LogInformation($"GetPriceFromTicker Request sent for Ticker {ticker}");

                using (HttpResponseMessage response = await _client.GetAsync(GetRequestURL(ticker)).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    _logger.LogInformation($"GetPriceFromTicker Response received for Ticker {ticker}, response was: {json}");

                    var responseObject = JsonSerializer.Deserialize<PriceCheckResponse>(json);

                    if (responseObject?.currentPrice is { } price)
                    {
                        _prices[ticker] = price;

                        if (_subscribers.Any())
                        {
                            var update = new PriceUpdate
                            {
                                Symbol = ticker,
                                Price = (double)price
                            };

                            try
                            {
                                foreach (var sub in _subscribers.ToArray())
                                {
                                    if (!sub.Writer.TryWrite(update))
                                    {
                                        await CleanUpSubscribers(sub);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to Publish new Price to Subscribers for Ticker {ticker}");
                            }
                        }
                    }
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
            return _client.BaseAddress.ToString().Replace("[Ticker]", ticker);
        }

        private Channel<PriceUpdate> CreateChannel()
        {
            var clientChannel = Channel.CreateBounded<PriceUpdate>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });

            _subscribers.Add(clientChannel);

            return clientChannel;
        }

        private async Task PublishExistingPrices(IServerStreamWriter<PriceUpdate> responseStream)
        {
            foreach (var kvp in _prices)
            {
                await responseStream.WriteAsync(new PriceUpdate
                {
                    Symbol = kvp.Key,
                    Price = (double)kvp.Value
                });
            }
        }
        private async Task PublishPriceUpdates(
            IServerStreamWriter<PriceUpdate> responseStream, 
            Channel<PriceUpdate> channel, 
            CancellationToken cancellationToken)
        {
            await foreach (var update in channel.Reader.ReadAllAsync(cancellationToken))
            {
                await responseStream.WriteAsync(update).ConfigureAwait(false);
            }
        }

        private async Task CleanUpSubscribers(Channel<PriceUpdate> clientChannel)
        {
            var newSubscribers = _subscribers.Where(x => x != clientChannel);
            _subscribers.Clear();
            foreach (var subscriber in newSubscribers) _subscribers.Add(subscriber);
        }

        public override async Task GetLatestPrices(
            Empty request,
            IServerStreamWriter<PriceUpdate> responseStream,
            ServerCallContext context)
        {
            var clientChannel = CreateChannel();
            try
            {
                await PublishExistingPrices(responseStream).ConfigureAwait(false);
                await PublishPriceUpdates(responseStream, clientChannel, context.CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Client disconnected from price stream.");
            }
            finally
            {
                await CleanUpSubscribers(clientChannel);
                clientChannel.Writer.TryComplete();
            }
        }
        public IDictionary<string, decimal> GetPrices()
        {
            return _prices;
        }
    }
}
