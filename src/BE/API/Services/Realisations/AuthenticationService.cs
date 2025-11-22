using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.IdentityModel.Tokens;

namespace API.Services.Realisations;

public class AuthenticationService(
    IGenericRepository<User> userRepository, 
    IConfiguration configuration)
    : IAuthenticationService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await userRepository.GetSingleAsync<User>(u => u.Email == request.Email);
        
        if (existingUser.Success && existingUser.Data != null)
        {
            return Result<AuthResponse>.Fail("User with this email already exists");
        }
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = passwordHash,
            TimeZone = request.TimeZone,
            Location = request.Location
        };
        var createResult = await userRepository.AddAsync(user);
       
        if (!createResult.Success)
        {
            return Result<AuthResponse>.Fail(createResult.Error);
        }
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());
        var response = new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Description = user.Description,
            TimeZone = user.TimeZone,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthResponse>.Ok(response);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var userResult = await userRepository.GetSingleAsync<User>(u => u.Email == request.Email);
        
        if (!userResult.Success || userResult.Data == null)
        {
            return Result<AuthResponse>.Fail("Invalid email or password");
        }
        var user = userResult.Data;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Result<AuthResponse>.Fail("Invalid email or password");
        }
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());
        var response = new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Description = user.Description,
            TimeZone = user.TimeZone,
            Location = user.Location,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthResponse>.Ok(response);
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtSettings["Issuer"] ?? "BookLoopAPI";
        var audience = jwtSettings["Audience"] ?? "BookLoopClient";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(GetTokenExpirationHours()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetTokenExpirationHours()
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        return int.TryParse(jwtSettings["ExpirationHours"], out var hours) ? hours : 24;
    }

    public async Task<Result<AuthResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var userResult = await userRepository.GetSingleAsync<User>(u => u.Id == userId);

        if (!userResult.Success || userResult.Data == null)
        {
            return Result<AuthResponse>.Fail("User not found");
        }
        var user = userResult.Data;

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.TimeZone = request.TimeZone ?? user.TimeZone;
        user.Location = request.Location ?? user.Location;
        user.Img = request.Img ?? user.Img;
        user.Description = request.Description ?? user.Description;
        
        var updateResult = await userRepository.UpdateAsync(user);

        if (!updateResult.Success)
        {
            return Result<AuthResponse>.Fail(updateResult.Error);
        }
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(GetTokenExpirationHours());
        var response = new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = token,
            ExpiresAt = expiresAt
        };

        return Result<AuthResponse>.Ok(response);
    }
}
