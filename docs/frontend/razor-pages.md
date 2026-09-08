# Razor Pages workspace

## Scope and architecture

The user approved a simple Razor Pages frontend after backend Phases 1–5.
This is an additive presentation layer, not a replacement or restructuring of
the authoritative backend contract in `docs/codex-implementation-spec.md`.

The existing `BPInventoryOps.Api` project now serves both `/api/*` JSON endpoints
and server-rendered browser pages. PageModels call the same scoped services as
controllers. They do not query the DbContext, call their own API over HTTP,
duplicate transactional business rules, or accept client-provided audit actors.
There are no new packages, projects in the solution, migrations, or Azure resources.

`Pages/Shared/UiPageModel` handles expected form errors and successful
POST/redirect/GET notifications. `PageLookups` obtains active catalog choices
from existing paginated services; history filters include inactive choices too.
These small-store dropdowns fetch successive pages of at most 100 records; large
catalogs would warrant server-side searchable selectors in a future enhancement.
List results still use the existing SQL filtering, allow-listed sorting, and pagination.

## Pages and permissions

| Pages | Access |
| --- | --- |
| `/Account/Login`, `/Account/AccessDenied`, `/Error` | Anonymous |
| `/`, `/Products`, `/Products/Details/{id}`, `/LowStock` | Employee+ |
| `/Categories`, `/Vendors` | Employee+ |
| `/Products/Edit/{id?}`, `/Categories/Edit/{id?}`, `/Vendors/Edit/{id?}` | Manager+ |
| `/Restocks`, `/Restocks/Create`, `/Restocks/Details/{id}` | Employee+ |
| `/Adjustments`, `/Adjustments/Create`, `/Adjustments/Details/{id}` | Employee+ |
| `/Audit`, `/Audit/Details/{id}` | Manager+ |
| `/Users`, `/Users/Create`, `/Users/Edit/{id}` | Admin |
| `/Account/Password`, `/Account/Logout` | Authenticated Employee+ |

Authorization conventions protect whole pages, including every POST handler.
Hidden navigation is a usability measure, not the authorization boundary.
Audit and stock-history pages provide no edit/delete operations.

## Authentication, safety, and form behavior

- Existing Identity authentication and policies are reused without JWT or a
  separate browser authentication scheme.
- Browser challenges redirect to login/access-denied pages. API challenges
  retain JSON ProblemDetails `401`/`403` responses.
- Login only follows local return URLs. Login failure messages remain generic.
- Razor Pages validates hidden antiforgery tokens on POST. The API antiforgery
  filter stays API-only. Tokens are regenerated from a fresh page after login.
- GET never mutates business state. All writes use POST forms and call services.
- Successful writes redirect; validation/conflict failures redisplay the form
  with safe messages and preserve non-secret input. Password fields are not echoed.
- Razor encodes displayed names, contact data, notes, and audit details. No raw
  user-generated HTML or external JavaScript/CDN is used.
- Browser responses prohibit caching and framing. Secure authentication cookies
  require HTTPS in local development as well as Azure.
- Unexpected errors use a safe HTML error page with a trace ID; API exceptions
  retain the existing ProblemDetails contract.
- Page list fields explicitly use the `Query.*` binding prefix so the pagination
  `Page` field cannot accidentally bind Razor's internal page route name.

The Restock form uses a small presentation model with a mutable `List` because
HTML collection binding does not bind the API DTO's `IReadOnlyList`. It maps
explicitly to the unchanged `CreateRestockRequest`. Line items reuse existing
validation DTOs. JavaScript adds/removes and reindexes lines; a server-side
Add Line handler also works without JavaScript. Only the save handler writes.

Received-at and history-filter inputs are explicitly UTC. Displayed product
quantities are snapshots; services revalidate stock and vendor/product status
inside their existing transaction workflow at save time. Product forms contain
no quantity-on-hand editor. New products still start at zero.

## Running and deployment

Use the existing SQL Express setup, migrations, and externally configured demo
passwords described in the README, then run in Windows PowerShell:

```powershell
dotnet run --project BPInventoryOps.Api/BPInventoryOps.Api.csproj --launch-profile https
```

Open `https://localhost:7104/`. This uses the same accounts as the API; seeding
does not reset existing passwords. Trust the local development HTTPS certificate
if necessary with `dotnet dev-certs https --trust`.

Azure is not modified by this frontend implementation. A subsequent publish of
the existing project includes compiled Razor Pages and `wwwroot` assets; deploy
it using the existing reviewed Phase 5 workflow. No SQL migration, additional
frontend service, CORS setup, or subscription/SKU change is needed.

## Verification

- The full automated suite includes the original 11 API tests and 8 new
  Razor Pages integration tests using isolated `BPInventory_Test_*` SQL Express
  databases and the real Identity/form-token pipeline.
- New coverage checks browser versus API challenges, missing form tokens,
  forged unauthorized page POSTs, catalog forms and stock preservation,
  protected deactivation, HTML encoding, pagination, multi-line delivery
  binding and duplicate rollback, adjustment rejection and actor attribution,
  Admin account lifecycle/final-Admin protection, password changes, and safe errors.
- Real headless Windows Edge checks use localhost HTTPS and a disposable
  database: manager login, dashboard, successful delivery form submission,
  JavaScript add/remove/reindex behavior, and mobile page-width checks.
- Desktop and mobile screenshots are generated under ignored local `artifacts/`;
  they are real rendered pages, not mockups. The temporary browser host and its
  database are stopped/removed after verification.

This is a usable small-store/demo interface, not a claim of comprehensive
accessibility certification or production-user acceptance testing.
