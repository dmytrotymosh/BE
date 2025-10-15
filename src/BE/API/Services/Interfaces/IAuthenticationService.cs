using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IAuthenticationService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<AuthResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
}
