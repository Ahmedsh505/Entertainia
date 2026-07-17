using Core.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Core.Interfaces.ServicesInterface;

namespace Service.Services
{
    /// <summary>
    /// Currency Conversion Service using ExchangeRate-API
    /// Free tier: 1,500 requests/month
    /// API: https://www.exchangerate-api.com/
    /// </summary>
    public class CurrencyConversionService : ICurrencyConversionService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private const string CacheKey = "ExchangeRates";
        private const int CacheExpirationHours = 24;
        private readonly IEgyptTimeService _timeService;

        // Free API key - replace with your own in appsettings.json
        private string ApiKey => _configuration["ExchangeRateApi:ApiKey"] ?? "YOUR_API_KEY_HERE";
        private string ApiBaseUrl => "https://v6.exchangerate-api.com/v6";

        public CurrencyConversionService(
            HttpClient httpClient,
            IMemoryCache cache,
            IConfiguration configuration,
            IEgyptTimeService timeService)
        {
            _httpClient = httpClient;
            _cache = cache;
            _configuration = configuration;
            _timeService = timeService;
        }

        public async Task<decimal> ConvertToEGPAsync(decimal amount, Currency fromCurrency)
        {
            if (fromCurrency == Currency.EGP)
                return amount;

            var rate = await GetExchangeRateAsync(fromCurrency, Currency.EGP);
            return amount * rate;
        }

        public async Task<decimal> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency)
        {
            if (fromCurrency == toCurrency)
                return 1.0m;

            try
            {
                var rates = await GetAllRatesAsync(fromCurrency);
                var toCurrencyCode = toCurrency.ToString();

                if (rates.TryGetValue(toCurrencyCode, out var rate))
                    return rate;

                // Fallback to fixed rates if API fails
                return GetFallbackRate(fromCurrency, toCurrency);
            }
            catch
            {
                return GetFallbackRate(fromCurrency, toCurrency);
            }
        }

        public async Task<Dictionary<string, decimal>> GetAllRatesAsync(Currency baseCurrency)
        {
            var cacheKey = $"{CacheKey}_{baseCurrency}";

            // Check cache first
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, decimal>? cachedRates) && cachedRates != null)
            {
                return cachedRates;
            }

            try
            {
                var currencyCode = baseCurrency.ToString();
                var url = $"{ApiBaseUrl}/{ApiKey}/latest/{currencyCode}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<ExchangeRateResponse>(json);

                    if (data?.conversion_rates != null)
                    {
                        // Cache for 24 hours
                        _cache.Set(cacheKey, data.conversion_rates, TimeSpan.FromHours(CacheExpirationHours));
                        return data.conversion_rates;
                    }
                }
            }
            catch (Exception)
            {
                // Log error and return fallback rates
            }

            // Return fallback rates if API call fails
            return GetFallbackRates(baseCurrency);
        }

        // Fallback fixed rates (approximate, updated periodically)
        private Dictionary<string, decimal> GetFallbackRates(Currency baseCurrency)
        {
            // Base rates in EGP (as of example)
            var egpRates = new Dictionary<string, decimal>
            {
                { "EGP", 1.0m },
                { "USD", 0.032m },    // 1 EGP ≈ 0.032 USD (31.25 EGP per USD)
                { "EUR", 0.030m },    // 1 EGP ≈ 0.030 EUR
                { "GBP", 0.026m },    // 1 EGP ≈ 0.026 GBP
                { "SAR", 0.12m },     // 1 EGP ≈ 0.12 SAR
                { "AED", 0.118m }     // 1 EGP ≈ 0.118 AED
            };

            if (baseCurrency == Currency.EGP)
                return egpRates;

            // Convert from base currency to all others
            var baseToEgp = 1.0m / egpRates[baseCurrency.ToString()];
            var rates = new Dictionary<string, decimal>();

            foreach (var kvp in egpRates)
            {
                rates[kvp.Key] = kvp.Value * baseToEgp;
            }

            return rates;
        }

        private decimal GetFallbackRate(Currency fromCurrency, Currency toCurrency)
        {
            var rates = GetFallbackRates(fromCurrency);
            return rates.TryGetValue(toCurrency.ToString(), out var rate) ? rate : 1.0m;
        }

        // Response model for ExchangeRate-API
        private class ExchangeRateResponse
        {
            public string? result { get; set; }
            public string? base_code { get; set; }
            public Dictionary<string, decimal>? conversion_rates { get; set; }
        }
    }
}