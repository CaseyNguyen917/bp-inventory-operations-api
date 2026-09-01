# Azure SQL Security and Networking

## 1. Authentication and Networking Are Separate Controls

A database connection must pass two different security questions.

### Network

Can this source reach the Azure SQL endpoint?

### Identity / authorization

If it can reach the endpoint, who is it and what database permissions does it have?

Managed identity solves authentication.

Firewall/private-network configuration solves network reachability.

One does not replace the other.

---

## 2. Runtime Authentication Decision

Azure App Service uses a system-assigned managed identity.

No SQL username/password is stored for normal runtime database access.

Conceptual Azure connection string:

```text
Server=tcp:<server>.database.windows.net,1433;
Database=BPInventory;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
```

Exact formatting can be adjusted to the current Microsoft.Data.SqlClient guidance.

The important property is:

No password is embedded.

---

## 3. Why System-Assigned Managed Identity?

Azure creates an identity attached to the App Service resource.

The application can authenticate to Azure SQL as that workload identity.

Benefits:

- no database password in Git
- no password in App Service settings
- no password rotation burden
- identity is tied to the web app lifecycle

When the App Service is deleted, its system-assigned identity is deleted with it.

---

## 4. Database User

Configure Microsoft Entra authentication for the Azure SQL logical server.

Inside the application database, create a contained user mapped to the App Service managed identity.

Conceptually:

```sql
CREATE USER [<app-service-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<app-service-name>];
ALTER ROLE db_datawriter ADD MEMBER [<app-service-name>];
```

The runtime application does not receive schema-administration rights.

---

## 5. Least Privilege

Runtime application identity needs normal application data permissions.

It should not normally have:

- db_owner
- broad server administration
- schema deployment rights

Schema changes use a separate deployment identity.

This creates a clear separation:

```text
Runtime identity
→ read/write application data

Deployment identity
→ apply approved schema migrations
```

---

## 6. SQL Network Access

MVP networking:

Azure SQL public network access is enabled only for selected networks/IP rules.

Do NOT enable the broad:

`Allow Azure services and resources to access this server`

switch for the final design.

That rule allows connectivity attempts from Azure resources beyond this subscription and is broader than necessary.

---

## 7. App Service Firewall Rules

App Service outbound connections originate from one of the web app's documented outbound IP addresses.

Azure SQL firewall rules must allow all relevant outbound IP addresses for the App Service.

Operational note:

Possible App Service outbound IPs can change when certain plan/resource changes occur.

After major App Service networking/plan changes, re-check the Azure SQL firewall allow-list.

---

## 8. Developer / Migration Access

For local administrative access to Azure SQL:

- add the current developer public IP to the SQL firewall temporarily
- authenticate using a Microsoft Entra identity with appropriate database/schema privileges
- remove stale IP rules when no longer needed

Do not expose Azure SQL to all IPv4 addresses.

---

## 9. Production Hardening Option

Future:

1. create Azure Virtual Network
2. configure App Service VNet Integration for outbound traffic
3. create Azure SQL Private Endpoint
4. configure Private DNS
5. disable Azure SQL public network access

This removes the public SQL endpoint from the application path.

It is not required for the MVP.

---

## 10. TLS

Azure SQL connections must remain encrypted.

Use modern TLS and:

`Encrypt=True`

Do not disable certificate validation just to solve connection errors.

`TrustServerCertificate=True` is not the production default for this architecture.

---

## 11. Interview Summary

> I use defense in depth for database access. Azure SQL networking only allows selected sources, while the App Service authenticates using a system-assigned managed identity instead of a SQL password. The managed identity is created as a database user with only runtime read/write permissions. Schema migrations use a separate deployment identity with elevated DDL rights, so the normal web application does not run as database owner. For an enterprise hardening step I would move the SQL path to VNet Integration and Private Link and disable public SQL networking.
