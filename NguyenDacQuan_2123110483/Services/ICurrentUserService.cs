namespace CoffeeHRM.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    string? IpAddress { get; }
}
