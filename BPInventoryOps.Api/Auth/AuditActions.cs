namespace BPInventoryOps.Api.Auth;

public static class AuditActions
{
    public const string ProductCreated = nameof(ProductCreated);
    public const string ProductUpdated = nameof(ProductUpdated);
    public const string ProductDeactivated = nameof(ProductDeactivated);
    public const string ProductReactivated = nameof(ProductReactivated);

    public const string CategoryCreated = nameof(CategoryCreated);
    public const string CategoryUpdated = nameof(CategoryUpdated);
    public const string CategoryDeactivated = nameof(CategoryDeactivated);
    public const string CategoryReactivated = nameof(CategoryReactivated);

    public const string VendorCreated = nameof(VendorCreated);
    public const string VendorUpdated = nameof(VendorUpdated);
    public const string VendorDeactivated = nameof(VendorDeactivated);
    public const string VendorReactivated = nameof(VendorReactivated);

    public const string RestockRecorded = nameof(RestockRecorded);
    public const string InventoryAdjusted = nameof(InventoryAdjusted);

    public const string UserCreated = nameof(UserCreated);
    public const string UserRoleChanged = nameof(UserRoleChanged);
    public const string UserDeactivated = nameof(UserDeactivated);
    public const string UserReactivated = nameof(UserReactivated);
    public const string PasswordChanged = nameof(PasswordChanged);
}
