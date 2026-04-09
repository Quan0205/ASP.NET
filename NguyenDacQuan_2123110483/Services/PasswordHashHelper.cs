using System.Security.Cryptography;
using System.Text;

namespace CoffeeHRM.Services;

public static class PasswordHashHelper
{
    private const string Salt = "CoffeeHRM::DemoSalt::v1";

    public static string Hash(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{Salt}:{password.Trim()}"));
        return Convert.ToBase64String(bytes);
    }

    public static bool Verify(string storedHash, string password)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var normalized = storedHash.Trim();
        if (string.Equals(normalized, password.Trim(), StringComparison.Ordinal))
        {
            return true;
        }

        var candidate = Hash(password);
        return string.Equals(normalized, candidate, StringComparison.Ordinal);
    }
}
