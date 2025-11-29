using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IRecommendationService
{
    Task<Result<RecommendationResponse>> GetRecommendationsAsync(Guid userId, RecommendationRequest request);
}
