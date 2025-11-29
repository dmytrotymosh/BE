using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IBookSwipeService
{
    Task<Result<IEnumerable<SwipeableBookResponse>>> GetSwipeableBooksAsync(Guid userId, int limit = 10);
    Task<Result<SwipeResponse>> SwipeBookAsync(Guid userId, SwipeRequest request);
    Task<Result<IEnumerable<SwipeResponse>>> GetUserLikesAsync(Guid userId);
}
