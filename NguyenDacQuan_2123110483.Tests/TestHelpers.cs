using CoffeeHRM.Data;
using CoffeeHRM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoffeeHRM.Tests;

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public int? UserId { get; set; } = 1;
    public string? Username { get; set; } = "tester";
    public string? IpAddress { get; set; } = "127.0.0.1";
}

internal static class TestDbFactory
{
    public static AppDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new AppDbContext(options, new FakeCurrentUserService());
        context.Database.EnsureCreated();
        return context;
    }
}
