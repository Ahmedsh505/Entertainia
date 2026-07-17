using Core.Enums;

namespace Core.Interfaces.ServicesInterface
{
    public interface ICurrencyConversionService
    {
        Task<decimal> ConvertToEGPAsync(decimal amount, Currency fromCurrency);
        Task<decimal> GetExchangeRateAsync(Currency fromCurrency, Currency toCurrency);
        Task<Dictionary<string, decimal>> GetAllRatesAsync(Currency baseCurrency);
    }
}