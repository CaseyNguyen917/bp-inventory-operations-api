namespace BPInventoryOps.Api.Auth;

public static class ApplicationRoles
{
    public const string Employee = nameof(Employee);
    public const string Manager = nameof(Manager);
    public const string Admin = nameof(Admin);

    public static readonly IReadOnlySet<string> All = new HashSet<string>(
        [Employee, Manager, Admin],
        StringComparer.OrdinalIgnoreCase);

    public static bool IsBusinessRole(string role)
    {
        return All.Contains(role);
    }

    public static bool TryNormalize(string? role, out string normalizedRole)
    {
        normalizedRole = role?.Trim() switch
        {
            string value when value.Equals(Employee, StringComparison.OrdinalIgnoreCase) => Employee,
            string value when value.Equals(Manager, StringComparison.OrdinalIgnoreCase) => Manager,
            string value when value.Equals(Admin, StringComparison.OrdinalIgnoreCase) => Admin,
            _ => string.Empty
        };

        return normalizedRole.Length > 0;
    }
}
