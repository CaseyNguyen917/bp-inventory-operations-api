namespace BPInventoryOps.Api.Auth;

public static class AuthorizationPolicies
{
    public const string EmployeeOrAbove = nameof(EmployeeOrAbove);
    public const string ManagerOrAbove = nameof(ManagerOrAbove);
    public const string AdminOnly = nameof(AdminOnly);
}
