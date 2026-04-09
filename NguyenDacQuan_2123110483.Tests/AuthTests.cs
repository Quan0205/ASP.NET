using CoffeeHRM.Services;

namespace CoffeeHRM.Tests;

public class AuthTests
{
    [Fact]
    public void PasswordHashHelper_ShouldHashAndVerifyPassword()
    {
        var hash = PasswordHashHelper.Hash("admin123");

        Assert.NotEqual("admin123", hash);
        Assert.True(PasswordHashHelper.Verify(hash, "admin123"));
        Assert.False(PasswordHashHelper.Verify(hash, "wrong-password"));
    }
}
