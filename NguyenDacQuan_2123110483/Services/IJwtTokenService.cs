using CoffeeHRM.Models;

namespace CoffeeHRM.Services;

public interface IJwtTokenService
{
    string CreateAccessToken(UserAccount account, IEnumerable<string> permissions, DateTime expiresAt);
    string CreateRefreshToken();
    string HashRefreshToken(string token);
}
