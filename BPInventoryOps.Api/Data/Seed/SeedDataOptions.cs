namespace BPInventoryOps.Api.Data.Seed;

public sealed class SeedDataOptions
{
    public const string SectionName = "SeedData";

    public bool Enabled { get; set; }

    public string? DemoEmployeePassword { get; set; }

    public string? DemoManagerPassword { get; set; }

    public string? DemoAdminPassword { get; set; }
}
