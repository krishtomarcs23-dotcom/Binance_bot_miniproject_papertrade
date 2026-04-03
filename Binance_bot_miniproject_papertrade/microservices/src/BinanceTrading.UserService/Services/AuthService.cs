using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BinanceTrading.Core.Interfaces;
using BinanceTrading.Core.Models;
using BinanceTrading.Core.Constants;
using BinanceTrading.UserService.Data;

namespace BinanceTrading.UserService.Services;

public class AuthService : IAuthService
{
    private readonly UserDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(UserDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponse> RegisterAsync(UserRegistrationRequest request)
    {
        var existingUser = await _context.Users
            .Find(u => u.Username == request.Username || u.Email == request.Email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
            throw new Exception("Username or email already exists");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = UserRoles.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.InsertOneAsync(user);

        return GenerateTokenResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(UserLoginRequest request)
    {
        var user = await _context.Users
            .Find(u => u.Username == request.Username)
            .FirstOrDefaultAsync();

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new Exception("Invalid username or password");

        if (!user.IsActive)
            throw new Exception("Account is deactivated");

        user.LastLoginAt = DateTime.UtcNow;
        await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);

        return GenerateTokenResponse(user);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId.ToString());
        return await _context.Users.Find(filter).FirstOrDefaultAsync();
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "DefaultSecretKey123456789012345678901234567890");

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = AppConstants.JwtIssuer,
                ValidateAudience = true,
                ValidAudience = AppConstants.JwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private AuthResponse GenerateTokenResponse(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? "DefaultSecretKey123456789012345678901234567890");
        var expires = DateTime.UtcNow.AddHours(AppConstants.JwtExpiryHours);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = expires,
            Issuer = AppConstants.JwtIssuer,
            Audience = AppConstants.JwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AuthResponse
        {
            Token = tokenHandler.WriteToken(token),
            Username = user.Username,
            Role = user.Role,
            ExpiresAt = expires
        };
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
