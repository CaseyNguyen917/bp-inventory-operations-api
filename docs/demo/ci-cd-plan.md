# CI/CD Plan

## Status

GitHub Actions is optional polish after the core application, tests, and Azure deployment work manually.

Do not delay the MVP backend for CI/CD.

## CI Goal

For pull requests and pushes to the main development branch:

```text
Checkout
→ Setup .NET
→ Restore
→ Build
→ Provision SQL Server test dependency
→ Apply migrations
→ Run xUnit suite
```

No Azure deployment occurs unless CI succeeds.

## GitHub Workflow Location

```text
.github/workflows/
├── ci.yml
└── deploy-azure.yml   # later, optional
```

## CI .NET SDK

Use the repository's target .NET version explicitly with `actions/setup-dotnet`.

The workflow should use the same major SDK as local development rather than depending on whatever SDK happens to be the runner default.

## SQL Server Integration Tests in CI

The primary automated suite requires SQL Server semantics.

Preferred CI approach:

- start an ephemeral SQL Server service/container for the workflow job
- wait for SQL Server readiness
- create/configure `BPInventory_Test`
- inject `ConnectionStrings__TestConnection`
- apply real EF migrations
- run `dotnet test`

Using a SQL Server container in CI does NOT mean the application is containerized in production. It is disposable test infrastructure only.

If GitHub-hosted SQL Server provisioning proves disproportionately complex, CI can initially run build + infrastructure-independent tests while the full SQL Server suite remains required locally. Full CI integration coverage should then be added before calling CI/CD complete.

## Build Commands

Conceptual sequence:

```text
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
```

Publish test results as an Actions artifact if useful.

## Branch Policy

Recommended once the project is stable:

- `main` should stay buildable
- feature work through focused branches
- CI runs on pull request and push
- do not merge known failing builds

Formal GitHub branch-protection rules are optional for a solo portfolio repository.

## Deployment Goal

Future deployment workflow:

```text
CI passes
→ authenticate GitHub to Azure via OIDC
→ produce Release publish artifact
→ apply reviewed EF migration bundle
→ deploy App Service
→ check /health/ready
→ smoke test
```

## Azure Authentication

Use OpenID Connect federation for GitHub Actions → Azure.

Do NOT prefer a long-lived Azure password or publish-profile credential when OIDC is available.

Conceptually:

```text
GitHub workflow identity
→ OIDC token
→ Microsoft Entra federated credential
→ short-lived Azure access
```

## Deployment Identity

GitHub deployment identity is distinct from the App Service runtime managed identity.

Runtime:

```text
App Service managed identity
→ normal DB data access
```

Deployment:

```text
GitHub federated identity / approved migration identity
→ Azure deployment + migration permissions
```

Apply least privilege to both.

## Migration Safety

Deployment must not blindly run schema changes before tests.

Order:

1. restore/build
2. tests
3. create publish artifact
4. create/review migration bundle
5. authenticate deployment identity
6. apply migration
7. deploy compatible application
8. verify readiness

For a solo MVP, migration review may be manual even if deployment is automated.

## Secrets

Do not store:

- SQL passwords
- Azure service-principal passwords
- demo account passwords

in workflow YAML.

OIDC removes the need for a long-lived Azure client secret.

Non-secret Azure IDs such as client/tenant/subscription identifiers may be stored as repository variables or secrets according to repository preference.

## Failure Behavior

If:

- build fails
- test fails
- migration fails
- deployment fails
- readiness fails

the workflow must fail visibly.

Do not report success while swallowing deployment errors.

## Interview Summary

> I added CI only after the backend was working locally so infrastructure automation didn't distract from core correctness. GitHub Actions restores, builds, provisions a disposable SQL Server test dependency, applies the real migrations, and runs xUnit integration tests. For Azure deployment I use OIDC federation so GitHub receives short-lived Azure credentials rather than storing a service-principal password. Deployment and runtime identities are separate, and database migrations occur as a deployment step rather than at normal application startup.
