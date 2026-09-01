# Security Configuration and Secrets

## 1. Configuration Sources

Use ASP.NET Core configuration instead of hard-coding environment-specific values.

Typical sources:

1. appsettings.json
2. appsettings.Development.json
3. .NET User Secrets in local development
4. environment variables
5. Azure App Service application settings / connection-string settings

Later sources override earlier sources.

---

# 2. appsettings.json

Allowed examples:

- logging levels
- non-secret pagination defaults
- public feature configuration
- cookie/session durations if desired

Do not commit:

- real passwords
- real production database credentials
- seed-user passwords
- API keys

---

# 3. Local Development Secrets

Use .NET User Secrets for sensitive development values when required.

Examples:

```text
ConnectionStrings:DefaultConnection
SeedUsers:AdminPassword
SeedUsers:ManagerPassword
SeedUsers:EmployeePassword
```

User Secrets live outside the project tree and aren't checked into Git.

They are a development convenience, not a production secrets vault.

---

# 4. Azure

For Azure deployment:

- configure Azure SQL connection information through App Service configuration
- configure demo/admin seed credentials through deployment settings only if seeding is enabled
- enforce HTTPS
- do not commit Azure credentials

Azure Key Vault may be introduced later if the project benefits from a dedicated secret store.

It is not required merely to make the architecture look more sophisticated.

---

# 5. Cookie Security

Production authentication cookie:

- HttpOnly = true
- Secure = true
- SameSite = Strict
- limited lifetime
- no sensitive payload exposed to JavaScript

Do not place passwords or unnecessary personal/business secrets inside claims/cookies.

---

# 6. CORS Configuration

No wildcard production CORS policy.

If a separate frontend is introduced:

```text
AllowedOrigins
```

should come from configuration.

Only explicitly trusted origins should be allowed.

Credentialed CORS must never be paired with an unrestricted origin wildcard.

---

# 7. Antiforgery

Configure a request header such as:

`X-CSRF-TOKEN`

A client obtains a valid token from:

`GET /api/auth/antiforgery-token`

Unsafe cookie-authenticated operations require the token.

---

# 8. HTTPS

Local:

- use HTTPS launch profile
- trust the local ASP.NET development certificate

Azure:

- enable HTTPS-only behavior
- production cookies require Secure transport

Do not transmit authentication credentials over HTTP.

---

# 9. Logging Rules

Never log:

- passwords
- PasswordHash
- authentication cookies
- antiforgery tokens
- database credentials
- connection-string passwords
- future access/refresh tokens

Avoid logging entire request bodies on authentication endpoints.

---

# 10. OpenAPI UI

Interactive API documentation is excellent for local development and demos.

Do not weaken production security solely so an interactive API UI can authenticate easily.

If production OpenAPI/interactive UI exposure is unnecessary, restrict or disable it outside Development/Demo environments.

The API contract itself remains documented in repository Markdown and generated OpenAPI.
