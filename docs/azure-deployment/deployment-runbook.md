# Azure Deployment Runbook

This is the planned deployment sequence. Exact Azure Portal labels may change over time.

## Phase A — Pre-deployment

1. `dotnet test` passes.
2. Repository is clean and pushed.
3. EF Core migrations are current.
4. Generate/review migration bundle.
5. Verify no credentials exist in Git.
6. Estimate Azure cost before resource creation.

---

## Phase B — Create Azure Resources

Create one resource group:

`rg-bpinventory-demo`

Create:

1. Azure App Service Plan
2. Azure App Service Web App
3. Azure SQL logical server
4. Azure SQL Database
5. Application Insights / Azure Monitor resource(s)

Use the same Azure region for app/database when practical.

---

## Phase C — App Service Configuration

1. Select supported .NET 10 runtime.
2. Enable HTTPS-only.
3. Set `ASPNETCORE_ENVIRONMENT=Production`.
4. Enable system-assigned managed identity.
5. Configure production app settings.
6. Set `APPLICATIONINSIGHTS_CONNECTION_STRING`.
7. Configure seed-data flag/credentials only if demo seeding is required.
8. Configure Health Check path to `/health/ready`.

---

## Phase D — Azure SQL Configuration

1. Configure Microsoft Entra administrator for the logical SQL server.
2. Set public network access to Selected networks.
3. Keep broad "Allow Azure services" disabled.
4. Add developer/admin IP temporarily when migration administration is needed.
5. Add all required App Service outbound IPs to SQL firewall rules.
6. Create managed-identity database user for the App Service.
7. Grant runtime data roles only.

---

## Phase E — Database Migration

1. Authenticate as deployment/database administrator.
2. Execute migration bundle.
3. Verify migration history.
4. Verify expected tables/constraints.
5. Remove unnecessary temporary firewall/admin access.

---

## Phase F — Application Deployment

Initial manual option:

- Visual Studio Publish to Azure App Service

or:

- standard App Service ZIP/deployment tooling

Future preferred automation:

- GitHub Actions with Azure OIDC federation

Deployment must publish built application output, not source-only assumptions.

---

## Phase G — Verification

Check:

1. App starts.
2. `/health` returns healthy.
3. `/health/ready` sees SQL.
4. Application Insights receives requests.
5. Demo seed succeeded if enabled.
6. Admin login works.
7. Employee/Manager/Admin permissions behave correctly.
8. Product list works.
9. Restock test changes inventory.
10. Invalid negative adjustment returns Conflict.
11. AuditLog records the operations.

---

## Phase H — Cost Control

1. Create Cost Management budget.
2. Configure notification thresholds.
3. Review Cost Analysis.
4. Confirm Azure SQL auto-pause behavior if serverless.
5. Scale down/delete unused resources.
6. Delete the resource group when the portfolio deployment is no longer needed.

Budget alerts notify; they do not automatically guarantee spending stops.
