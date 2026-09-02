# Phase 5 Azure Deployment Record

## 1. Scope and Status

Phase 5 (Milestones 16–17) was deployed and verified on September 2, 2026.
This is a portfolio/demo environment containing synthetic data only. CI/CD was
not added because Milestone 18 is outside Phase 5.

The active subscription remained `Azure for Students` throughout deployment.
No subscription switch was performed.

## 2. Deployed Resources

| Resource | Name | Region / tier |
| --- | --- | --- |
| Resource group | `rg-bpinventory-demo` | Metadata location: Central US |
| App Service plan | `asp-bpinventory-demo` | West US 3, Linux Free |
| Web App | `bpinventoryops-api-kc` | West US 3, .NET 10 |
| Azure SQL logical server | `bpinventoryops-sql-kc` | West US 3 |
| Azure SQL database | `BPInventory` | General Purpose serverless, `GP_S_Gen5_2` |
| Log Analytics workspace | `log-bpinventory-demo` | West US 3, `PerGB2018` |
| Application Insights | `appi-bpinventory-demo` | West US 3, workspace based |

The resource group had already been created with Central US metadata before a
subscription policy was found to restrict workload deployment to a smaller set
of regions. The workload resources use West US 3, which was policy-allowed and
offered the required services. Resource-group metadata location does not force
the contained resources to use that location.

The deployed API hostname is:

```text
https://bpinventoryops-api-kc.azurewebsites.net
```

## 3. App Service Configuration

The Web App has:

- Linux .NET 10 runtime (`DOTNETCORE|10.0`);
- system-assigned managed identity enabled;
- HTTPS-only enabled;
- minimum TLS 1.2;
- FTP disabled;
- `/health/ready` configured as the App Service Health Check path;
- `AlwaysOn=false`, as expected for the Free plan.

Production configuration is supplied through App Service settings. The
configured key names are:

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__DefaultConnection
APPLICATIONINSIGHTS_CONNECTION_STRING
SeedData__Enabled
SeedData__DemoEmployeePassword
SeedData__DemoManagerPassword
SeedData__DemoAdminPassword
```

The three passwords were entered interactively and were never printed or
committed. The deployed connection string contains no SQL username or password.
It uses `Authentication=Active Directory Default`, encryption, certificate
validation, and a 120-second connection timeout. The longer timeout accommodates
the first connection while the free serverless database resumes after auto-pause.

## 4. Azure SQL Security and Networking

The Web App's system-assigned managed identity is mapped to the `BPInventory`
database as the external user `bpinventoryops-api-kc`. It has only:

```text
db_datareader
db_datawriter
```

It does not have `db_owner` or schema-deployment rights. The developer's
Microsoft Entra identity was used separately for schema deployment.

Azure SQL public networking is enabled with selected firewall rules:

- all 32 documented possible outbound IPs for this App Service are allowed;
- the broad `Allow Azure services and resources to access this server` rule is
  disabled;
- the temporary developer migration IP rule was removed after provisioning;
- no broad IPv4 range was added;
- minimum SQL TLS is 1.2.

Re-check the possible outbound IP list and SQL firewall rules after an App
Service plan, scale, or networking change.

## 5. Database Migration and Demo Seed

The EF Core migration bundle was generated from the tracked migrations and run
manually with the separate deployment identity. Azure SQL migration history
contains:

```text
20260901172024_InitialCreate
```

The application does not call `Database.Migrate()` during Production startup.
The runtime managed identity therefore needs no DDL permission.

`SeedData__Enabled=true` initialized the idempotent synthetic demo dataset and
the Employee, Manager, and Admin demo accounts. Password values remain external
to source control.

## 6. Deployment and Runtime Verification

The application was published in Release mode and deployed as built ZIP output.
An initial startup attempt encountered a database post-login timeout while the
serverless database resumed. Increasing only the runtime SQL connection timeout
from 30 to 120 seconds resolved it; no EF retry behavior or transaction design
was changed.

The deployed system passed these checks:

| Check | Result |
| --- | --- |
| `/health` | HTTP 200, Healthy |
| `/health/ready` | HTTP 200, SQL Healthy |
| Unauthenticated `/api/products` | HTTP 401 ProblemDetails |
| Production OpenAPI document | HTTP 404 |
| Employee login and Product list | Passed; 23 synthetic Products |
| Employee low-stock list | Passed; 6 active low-stock Products |
| Multi-item Restock | Passed; both Product quantities increased exactly |
| Damage Adjustment | Passed; quantity decreased from 13 to 11 |
| Negative-stock Adjustment | HTTP 409; quantity remained 11 |
| Employee Product mutation | HTTP 403 |
| Manager Product management | Passed; threshold changed while quantity stayed 11 |
| Manager AuditLog access | Restock, Adjustment, and Product audit entries found |
| Manager user administration | HTTP 403 |
| Admin user administration | Passed; Employee role changed and restored |

The role test restored `employee@bp-inventory.demo` to the Employee role before
completion.

Application Insights verification showed:

- successful `GET /health/ready` request telemetry;
- successful SQL dependency telemetry targeting `BPInventory` on
  `bpinventoryops-sql-kc.database.windows.net`.

## 7. Cost Controls

Cost controls configured for the demo environment are:

- App Service Linux Free plan;
- Azure SQL free-limit serverless behavior enabled;
- Azure SQL minimum capacity 0.5 vCore and 60-minute auto-pause;
- Log Analytics retention of 30 days;
- Log Analytics daily ingestion quota of 0.1 GB;
- Application Insights daily cap of 0.1 GB;
- resource-group monthly budget `budget-bpinventory-demo-monthly` for USD 5;
- actual-cost email notifications at 80% and 100%.

The budget sends notifications but does not stop resources or guarantee a hard
spending ceiling. Review Cost Analysis regularly and delete
`rg-bpinventory-demo` when the portfolio deployment is no longer needed.

## 8. Operational Deployment Sequence

For a future manual redeployment:

1. run the Release build and automated tests;
2. review and generate an EF migration bundle when migrations changed;
3. temporarily allow the deployment operator's IP if database administration is
   required;
4. apply migrations using the separate deployment identity;
5. remove temporary database network access;
6. publish and ZIP the built application output;
7. deploy the ZIP to the existing Web App;
8. verify health, authentication, authorization, inventory workflows, AuditLog,
   and telemetry;
9. review the budget, SQL free-limit configuration, and current costs.

Do not grant schema rights to the runtime managed identity and do not add a
Production startup migration call.
