using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IExchangeService
{
    Task<Result<IEnumerable<ExchangeResponse>>> GetExchangesAsync(ExchangeFilterRequest request = null);
    Task<Result<ExchangeResponse>> GetExchangeByIdAsync(Guid exchangeId);
    Task<Result<ExchangeDetailsResponse>> GetExchangeDetailsAsync(Guid exchangeId, Guid userId);
    Task<Result<ExchangeResponse>> AddExchangeAsync(Guid ownerId, AddExchangeRequest request);
    Task<Result<ExchangeResponse>> UpdateExchangeAsync(Guid exchangeId, Guid userId, UpdateExchangeRequest request);
    Task<Result> DeleteExchangeAsync(Guid exchangeId, Guid userId);
}