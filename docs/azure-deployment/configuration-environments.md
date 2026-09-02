# Configuration and Environment Strategy

## 1. Purpose

The BP Inventory backend must run the same application code in multiple environments without hard-coded machine-specific or cloud-specific values.

The application uses the standard ASP.NET Core configuration system.

Environments:

- Development
- Testing
- Production

The public Azure portfolio deployment uses the Production environment, with a separate explicit demo-data flag where needed.

---

## 2. Configuration Sources

The application relies on the normal ASP.NET Core configuration pipeline.

Important sources include:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. .NET User Secrets in local Development
4. environment variables
5. command-line overrides when explicitly used

Later configuration providers override earlier values.

Environment variables therefore override values committed in `appsettings.json`.

---

## 3. appsettings.json

Contains shared, non-secret defaults.

Appropriate examples:

- logging configuration
- allowed pagination defaults
- non-secret feature configuration
- health-check behavior
- OpenAPI feature flags if needed

Never commit:

- production database passwords
- demo account passwords
- private keys
- API secrets

---

## 4. appsettings.Development.json

Contains Development-only non-secret overrides.

Examples:

- more verbose logging
- development OpenAPI behavior
- developer-friendly diagnostics

It must not become a secret store.

---

## 5. appsettings.Production.json

Optional.

Use only for non-secret Production defaults that should live in source control.

Actual Azure deployment values should normally come from App Service environment variables/settings.

---

## 6. Development Environment

Runtime:

`ASPNETCORE_ENVIRONMENT=Development`

Database:

Local SQL Server Developer/Express instance.

Preferred local authentication:

- Windows/Integrated authentication where available
- local SQL credentials only if necessary

Sensitive local values:

.NET User Secrets.

Example secret keys:

```text
ConnectionStrings:DefaultConnection
SeedData:DemoEmployeePassword
SeedData:DemoManagerPassword
SeedData:DemoAdminPassword
```

---

## 7. Testing Environment

Automated integration tests use a dedicated SQL Server test database.

Example:

`BPInventory_Test`

Configuration:

`ConnectionStrings:TestConnection`

Test configuration must never point to Production or the normal Development database.

Testing code may override the normal DbContext registration through the test host.

---

## 8. Azure Production/Demo Environment

Runtime:

`ASPNETCORE_ENVIRONMENT=Production`

Azure App Service provides environment-specific configuration.

Example environment-variable names:

```text
ConnectionStrings__DefaultConnection
SeedData__Enabled
SeedData__DemoEmployeePassword
SeedData__DemoManagerPassword
SeedData__DemoAdminPassword
APPLICATIONINSIGHTS_CONNECTION_STRING
```

Double underscore maps to nested ASP.NET Core configuration.

App Service values override committed JSON settings.

---

## 9. Demo Mode

Do not create a fake ASP.NET environment called `Demo` merely to trigger portfolio seed data.

Use:

`ASPNETCORE_ENVIRONMENT=Production`

plus an explicit flag:

`SeedData__Enabled=true`

Why?

Production framework behavior should remain Production behavior:

- production error handling
- secure cookies
- production logging defaults
- HTTPS/security behavior

Demo data is a business/deployment concern, not a hosting environment.

The current seeder validates all three configured demo passwords whenever
`SeedData__Enabled=true`, including after the users already exist. Keep those
settings present while demo seeding remains enabled, or disable the seed flag
after deciding that startup should no longer run the idempotent seeder.

---

## 10. Strongly Typed Configuration

Use the Options pattern for grouped settings that deserve structure.

Examples:

- SeedDataOptions
- future frontend/CORS options
- future application feature options

Do not create Options classes for every single configuration value.

---

## 11. launchSettings.json

`launchSettings.json` is Development tooling.

It may define:

- local URLs
- Development environment
- browser launch behavior

It is not an Azure deployment configuration file.

Azure does not depend on Visual Studio launch profiles.

---

## 12. Configuration Security

Configuration is not automatically secret merely because it is outside code.

For example:

`APPLICATIONINSIGHTS_CONNECTION_STRING`

should be supplied through environment configuration.

Never log the complete configuration tree.

Never expose configuration values through health endpoints or API responses.

---

## 13. Interview Summary

> The same ASP.NET Core application runs across Development, Testing, and Azure Production by using the built-in configuration provider chain rather than hard-coded values. Shared non-secret defaults live in appsettings.json, Development secrets use .NET User Secrets, tests override the database with a dedicated SQL Server test connection, and Azure App Service injects Production settings as environment variables. Environment variables override JSON configuration, so deployment can change database and telemetry settings without rebuilding the application.
