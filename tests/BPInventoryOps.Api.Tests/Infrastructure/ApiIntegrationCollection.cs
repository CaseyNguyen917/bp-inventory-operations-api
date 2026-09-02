using Xunit;

namespace BPInventoryOps.Api.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationCollection : ICollectionFixture<ApiTestFixture>
{
    public const string Name = "SQL Server API integration";
}
