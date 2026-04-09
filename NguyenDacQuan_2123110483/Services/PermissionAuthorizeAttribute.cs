using Microsoft.AspNetCore.Authorization;

namespace CoffeeHRM.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public PermissionAuthorizeAttribute(string permission)
    {
        Permission = permission;
    }

    public string Permission
    {
        get => Policy is { Length: > 11 } ? Policy[PolicyPrefix.Length..] : string.Empty;
        set => Policy = $"{PolicyPrefix}{value}";
    }
}
