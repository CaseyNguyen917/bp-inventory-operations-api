# Application Architecture

## 1. Purpose

This document defines the application architecture for the BP Franchise Inventory & Operations Management System.

The project will be implemented as a single ASP.NET Core Web API backed by SQL Server and Entity Framework Core.

The goal is to keep the architecture:

- easy to understand
- easy to test
- easy to explain in interviews
- appropriate for a single-store business application
- structured enough to preserve separation of concerns
- simple enough to avoid unnecessary enterprise boilerplate

The system is intentionally a modular monolith rather than a microservice architecture.

---

## 2. High-Level Architecture

```text
Client / Swagger / API Consumer
            |
            v
      ASP.NET Core API
            |
            v
       Controllers
            |
            v
        Services
            |
            v
   ApplicationDbContext
            |
            v
   Entity Framework Core
            |
            v
        SQL Server
```

Cross-cutting concerns such as authentication, authorization, exception handling, logging, configuration, and OpenAPI documentation surround this request flow.

---

## 3. Architectural Style

### 3.1 Modular monolith

The application is deployed as one backend application and uses one relational database.

"Monolith" does not mean unstructured code.

The code is separated into clear responsibilities and modules while remaining one deployable application.

This is appropriate because:

- the business scope is small
- the domain is closely related
- there is no independent scaling requirement
- there is no separate team ownership requirement
- distributed-system complexity would provide little value
- one deployment is easier to operate and understand

### 3.2 Why not microservices?

Microservices would introduce:

- network communication between services
- distributed transactions
- service discovery
- additional deployments
- more authentication boundaries
- additional logging/monitoring requirements
- data ownership decisions
- message/event infrastructure
- greater operational cost

None of those solve an actual MVP problem.

The architecture can still be modular enough that future extraction would be possible if real requirements justified it.

---

## 4. Layer Responsibilities

## 4.1 Controllers

Controllers form the HTTP/API boundary.

Controllers are responsible for:

- defining routes
- receiving HTTP requests
- model binding
- accepting request DTOs
- invoking application services
- returning appropriate HTTP responses
- applying authorization attributes/policies
- remaining thin

Controllers should not contain significant business logic.

Bad controller responsibility:

```text
Read product
validate inventory
create restock
calculate all stock changes
start database transaction
update several entities
write audit entry
save everything
```

Preferred controller responsibility:

```text
receive request
call RestockService.RecordAsync(...)
return result
```

This keeps HTTP concerns separate from business behavior.

---

## 4.2 Services

Services form the business/application layer.

Services are responsible for:

- business rules
- workflow orchestration
- validation that depends on stored state
- coordinating multiple entities
- inventory mutation
- transaction boundaries
- mapping entities to response DTOs where appropriate
- creating audit records where the business operation requires them
- throwing/returning meaningful domain/application failures

Examples:

### ProductService

- create Product
- ensure SKU uniqueness
- update allowed product fields
- deactivate Product
- retrieve low-stock products

### RestockService

- validate vendor
- validate products
- validate quantities
- create RestockEvent
- create RestockItems
- increase Product.QuantityOnHand
- commit all changes atomically

### InventoryAdjustmentService

- validate Product
- validate QuantityChange
- calculate resulting stock
- prevent negative inventory
- create InventoryAdjustment
- update Product.QuantityOnHand
- commit atomically

The service layer is the main location for business logic.

---

## 4.3 Data access / ApplicationDbContext

`ApplicationDbContext` is the Entity Framework Core gateway to SQL Server.

Responsibilities:

- expose `DbSet<T>` collections for persistent entities
- configure entity relationships
- configure indexes and constraints
- provide EF Core change tracking
- perform queries
- persist changes
- participate in database transactions

The DbContext should not contain HTTP logic.

It should not become a dumping ground for business rules.

---

## 4.4 Domain / persistence entities

Entities represent persistent domain data.

Examples:

- Product
- Category
- Vendor
- RestockEvent
- RestockItem
- InventoryAdjustment
- AuditLog
- ApplicationUser

Entities map to the relational schema through EF Core.

Entities are not automatically API contracts.

The API should not expose EF entities directly.

---

## 4.5 DTOs / API contracts

DTO means Data Transfer Object.

DTOs represent the data crossing the API boundary.

Examples:

- CreateProductRequest
- UpdateProductRequest
- ProductResponse
- CreateRestockRequest
- RestockItemRequest
- InventoryAdjustmentRequest
- LowStockProductResponse

DTOs allow the API contract to differ from the database entity.

Example:

The Product entity may contain:

- Id
- CreatedAtUtc
- UpdatedAtUtc
- navigation properties
- internal persistence state

A CreateProductRequest should contain only fields the client is allowed to submit.

This prevents over-posting and accidental exposure of internal fields.

---

## 5. Entity vs DTO

### Entity

Represents data as persisted and related inside the application.

Example conceptual Product entity:

```text
Id
Name
Sku
CategoryId
PrimaryVendorId
QuantityOnHand
ReorderThreshold
Cost
RetailPrice
IsActive
CreatedAtUtc
UpdatedAtUtc
Category navigation property
PrimaryVendor navigation property
```

### CreateProductRequest DTO

Might contain:

```text
Name
Sku
CategoryId
PrimaryVendorId
ReorderThreshold
Cost
RetailPrice
```

Notice what is missing:

- Id
- QuantityOnHand
- IsActive
- CreatedAtUtc
- UpdatedAtUtc

The server owns those values.

This is deliberate API design.

---

## 6. Why Entities Are Not Returned Directly

Returning EF entities directly can cause:

- accidental exposure of internal fields
- tight coupling between database schema and API contract
- navigation-property serialization problems
- circular references
- over-posting risks on writes
- difficulty versioning the API independently
- accidental breaking changes when persistence models change

DTOs establish a deliberate boundary.

---

## 7. Repository Pattern Decision

### Decision

The MVP will NOT introduce a generic repository layer.

Services will use `ApplicationDbContext` directly.

### Why?

Entity Framework Core `DbContext` already provides behavior similar to:

- repository access through `DbSet<T>`
- unit-of-work behavior through change tracking and `SaveChanges`

Adding:

```text
IRepository<T>
GenericRepository<T>
```

would mostly wrap EF Core methods with another abstraction.

Example:

```text
service
  -> IRepository<Product>
       -> DbContext.Products
```

instead of:

```text
service
  -> DbContext.Products
```

For this project, the extra layer does not solve a concrete problem.

### When might repositories be justified?

A repository abstraction could become useful if:

- data access becomes unusually complex
- persistence implementation must be swapped
- domain logic must be isolated from EF Core more aggressively
- several data sources must be combined
- reusable query objects are needed
- project architecture evolves into stronger domain/application/infrastructure boundaries

The decision can be revisited if a real need appears.

---

## 8. Dependency Injection

ASP.NET Core provides a built-in dependency injection container.

Dependencies are registered during application startup and requested by classes through constructors.

Conceptual example:

```text
ProductController
        |
        needs
        v
IProductService
        |
implemented by
        v
ProductService
```

The controller should not construct the service itself.

Bad:

```text
new ProductService(...)
```

Preferred:

```text
constructor requests IProductService
ASP.NET Core supplies ProductService
```

Benefits:

- loose coupling
- easier testing
- centralized dependency configuration
- explicit dependencies
- easier replacement of implementations

---

## 9. Service Interfaces

Application services will generally have interfaces.

Examples:

- IProductService
- ICategoryService
- IVendorService
- IRestockService
- IInventoryAdjustmentService
- IAuditService

Interfaces are useful here because controllers depend on behavior rather than concrete implementation.

They also make service substitution straightforward in unit tests.

Avoid creating interfaces for every trivial class merely as a ritual.

---

## 10. Dependency Lifetimes

ASP.NET Core services can use different lifetimes.

### Transient

A new instance is created each time it is requested.

### Scoped

One instance is created per dependency-injection scope.

For normal ASP.NET Core HTTP requests, that typically means one instance per request.

### Singleton

One instance lives for the application lifetime.

### Project decision

- `ApplicationDbContext`: Scoped
- business services: Scoped
- stateless singleton infrastructure only when clearly safe
- avoid Singleton services that directly depend on a scoped DbContext

EF Core registers DbContext as scoped by default through `AddDbContext`.

---

## 11. Program.cs as Composition Root

`Program.cs` is the application startup/composition root.

It is responsible for wiring the system together.

Conceptually it will:

1. create the application builder
2. register controllers
3. register OpenAPI
4. register ProblemDetails/error infrastructure
5. load configuration
6. register ApplicationDbContext
7. register application services
8. later register Identity/authentication/authorization
9. build the app
10. configure middleware
11. map controllers
12. run the application

Program.cs should configure components, not contain business workflows.

---

## 12. Middleware Pipeline

Middleware is software that participates in processing HTTP requests and responses.

Conceptually:

```text
HTTP Request
    |
Exception Handling
    |
HTTPS / Routing
    |
Authentication
    |
Authorization
    |
Controller Endpoint
    |
HTTP Response
```

Middleware order matters because each component wraps or forwards the request to the next component.

Examples of cross-cutting middleware responsibilities:

- exception handling
- HTTPS redirection
- authentication
- authorization

The application should not create custom middleware unless a real cross-cutting requirement needs it.

---

## 13. Centralized Exception Handling

Controllers should not contain repetitive try/catch blocks for every endpoint.

The application will use centralized exception handling.

Preferred direction:

- `AddProblemDetails`
- ASP.NET Core exception handling middleware
- custom `IExceptionHandler` implementation(s) when application-specific mapping is needed

Examples of application failures:

- product not found
- duplicate SKU
- invalid restock
- inventory would become negative
- invalid vendor

These can be mapped centrally to appropriate HTTP error responses.

Benefits:

- consistent API errors
- less duplicated controller code
- easier logging
- clearer separation of concerns

---

## 14. Problem Details

HTTP API errors should use a consistent problem-details response format where appropriate.

A problem response can communicate:

- HTTP status
- title
- detail/message
- error type
- trace identifier
- optional validation information

This is preferable to returning arbitrary error shapes from every controller.

---

## 15. Request Validation

Validation exists at multiple levels.

### DTO/input validation

Used for rules that can be evaluated from the submitted request alone.

Examples:

- Name is required
- SKU is required
- ReorderThreshold cannot be negative
- Cost cannot be negative

The MVP can use built-in ASP.NET Core model validation / DataAnnotations for straightforward DTO validation.

### Service/domain validation

Used for rules requiring database state or workflow context.

Examples:

- SKU must not already exist
- Category must exist
- Vendor must exist
- inventory adjustment must not make stock negative
- restock products must exist

### Database constraints

Used as the final integrity boundary.

Examples:

- unique SKU
- non-negative quantity
- foreign keys

This is defense in depth.

---

## 16. Configuration

ASP.NET Core configuration can load values from sources such as:

- `appsettings.json`
- `appsettings.Development.json`
- environment variables
- development user secrets
- Azure App Service application settings

The code should not hard-code environment-specific values.

Examples of configuration:

- SQL Server connection string
- JWT/authentication settings later
- logging settings
- future Azure integration settings

Production secrets must not be committed to Git.

---

## 17. Options Pattern

When a group of application settings deserves a strongly typed representation, the application should use the options pattern.

Instead of spreading string-based configuration lookups across controllers:

```text
Configuration["Something:Setting"]
```

define a settings/options class and bind configuration to it.

This improves:

- encapsulation
- testability
- validation
- discoverability
- separation of concerns

Not every setting needs a custom options class.

The connection string can be read during startup when registering DbContext.

---

## 18. Logging

ASP.NET Core provides structured logging through `ILogger<T>`.

Services can log important diagnostic events.

Examples:

- unexpected failures
- important workflow failures
- application startup information

Logging is not the same as AuditLog.

### Logging

Operational/technical diagnostics.

### AuditLog

Business accountability/history.

Do not use normal application logs as the only audit mechanism.

---

## 19. OpenAPI / Interactive API Documentation

The application will generate an OpenAPI document.

This provides a machine-readable contract describing endpoints, parameters, request models, response models, and HTTP methods.

During development, an interactive UI such as Scalar or Swagger UI may consume the OpenAPI document.

The API documentation layer is development/demo tooling, not business logic.

---

## 20. Request Lifecycle Example: Record Restock

Conceptual request:

```text
POST /api/restocks
```

Flow:

```text
Client
  |
  v
ASP.NET Core middleware
  |
  v
RestocksController
  |
  v
IRestockService
  |
  v
RestockService
  |
  +--> validate Vendor
  |
  +--> validate Products/quantities
  |
  +--> start/participate in transaction
  |
  +--> create RestockEvent
  |
  +--> create RestockItems
  |
  +--> update Product.QuantityOnHand
  |
  +--> create audit record
  |
  +--> SaveChanges
  |
  v
ApplicationDbContext
  |
  v
SQL Server
```

The service returns a result/DTO.

The controller translates that into the HTTP response.

---

## 21. Request Lifecycle Example: Low-Stock Report

Conceptual request:

```text
GET /api/products/low-stock
```

Flow:

```text
Client
  |
Controller
  |
ProductService
  |
ApplicationDbContext
  |
EF Core query
  |
SQL Server

WHERE QuantityOnHand <= ReorderThreshold
```

The resulting entities/projection are mapped into response DTOs and returned.

---

## 22. Testing Implications

This architecture intentionally keeps business logic outside controllers.

Therefore later tests can target:

```text
InventoryAdjustmentService
```

without needing to simulate every HTTP concern.

Examples:

- adjustment cannot create negative inventory
- restock increases all relevant product quantities
- duplicate SKU rejected
- low-stock query returns correct products

Controller/integration tests can separately verify HTTP routing and status codes.

---

## 23. Architecture Boundaries

### Controllers may depend on

- service interfaces
- API DTOs
- authorization infrastructure
- logging when needed

### Services may depend on

- ApplicationDbContext
- other narrowly required services
- mapping/helpers when needed
- logger
- current-user abstraction later if required

### Data layer may depend on

- EF Core
- entity configurations
- SQL Server provider

### Entities should not depend on

- controllers
- HTTP
- request/response types

Dependencies should generally flow inward from HTTP concerns toward application/domain/data behavior, not the reverse.

---

## 24. What We Are Not Building

The initial architecture will not include:

- microservices
- message bus
- CQRS framework
- MediatR
- event sourcing
- generic repository
- UnitOfWork wrapper around DbContext
- AutoMapper unless mapping becomes painful
- Redis
- distributed cache
- Docker orchestration
- Kubernetes
- separate domain/application/infrastructure class-library projects
- unnecessary design-pattern abstractions

These may be useful in other systems, but they are not requirements here.

---

## 25. Interview Summary

A concise explanation:

> I implemented the backend as a modular monolith using ASP.NET Core Web API. Controllers stay thin and handle HTTP concerns, while scoped application services contain business rules and orchestrate workflows. Services use EF Core's scoped DbContext directly instead of adding a generic repository abstraction, because DbContext and DbSet already provide unit-of-work and repository-like behavior for this project's needs. I separate persistence entities from API DTOs so clients can't directly control internal fields or couple the API contract to the database schema. Cross-cutting concerns such as exception handling, ProblemDetails, authentication, authorization, configuration, logging, and OpenAPI are handled through ASP.NET Core infrastructure rather than duplicated in controllers.
