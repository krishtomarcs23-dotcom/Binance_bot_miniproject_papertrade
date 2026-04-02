using BinanceTrading.Core.Models;

namespace BinanceTrading.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> ExistsAsync(string username, string email);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(UserRegistrationRequest request);
    Task<AuthResponse> LoginAsync(UserLoginRequest request);
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> ValidateTokenAsync(string token);
}

public interface IApiKeyService
{
    Task<UserApiKeys> AddApiKeyAsync(int userId, ApiKeyRequest request);
    Task<List<UserApiKeys>> GetUserApiKeysAsync(int userId);
    Task<bool> DeleteApiKeyAsync(int userId, int apiKeyId);
    Task<UserApiKeys?> GetActiveApiKeyAsync(int userId);
}
