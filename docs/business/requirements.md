# System Requirements

## 1. Purpose

This document defines the functional requirements, business rules, actors, permissions, and core workflow expectations for the BP Franchise Inventory & Operations Management System.

The requirements are intended to guide later database design, API design, service-layer behavior, testing, authentication, and deployment decisions.

---

## 2. Actors

### 2.1 Employee

An Employee represents a normal operational user.

Expected capabilities:

- View products.
- View current inventory quantities.
- View low-stock products.
- Record vendor restocks.
- Record permitted inventory adjustments.
- View information required to perform normal inventory operations.

### 2.2 Manager

A Manager represents an operational administrator.

Expected capabilities:

- Perform all Employee operations.
- Create products.
- Update products.
- Deactivate products.
- Create, update, and deactivate categories.
- Create, update, and deactivate vendors.
- Review inventory history.
- Review audit information.
- Perform privileged inventory corrections.

### 2.3 Admin

An Admin represents an application administrator.

Expected capabilities:

- Manage users.
- Assign and manage roles.
- Perform privileged administrative operations.
- Retain access to Manager-level capabilities where appropriate.

---

## 3. Functional Requirements

## 3.1 Product Management

The system shall allow authorized users to:

- Create a product.
- Retrieve all products.
- Retrieve a product by identifier.
- Update product details.
- Deactivate a product.
- View the product's current quantity on hand.
- View the product's reorder threshold.
- Associate a product with one category.
- Associate a product with one primary vendor.

A normal product update operation should not be used to arbitrarily change inventory quantity.

Inventory quantity changes should normally occur through restocks or inventory adjustments so that the reason for the change remains traceable.

---

## 3.2 Category Management

The system shall allow authorized users to:

- Create a category.
- Retrieve categories.
- Retrieve a category by identifier.
- Update a category.
- Deactivate a category.

A category that is referenced by historical or active product data should generally be deactivated rather than permanently deleted.

---

## 3.3 Vendor Management

The system shall allow authorized users to:

- Create a vendor.
- Retrieve vendors.
- Retrieve a vendor by identifier.
- Update vendor information.
- Deactivate a vendor.

For the MVP:

- A product has one primary vendor.
- A vendor may supply many products.

Multi-vendor-per-product support is intentionally excluded from the MVP.

---

## 3.4 Restock Management

The system shall allow authorized users to record incoming vendor deliveries.

A restock shall include:

- Vendor.
- Date and time received or recorded.
- User who recorded the restock.
- One or more restock line items.
- Optional notes.

Each restock line item shall include:

- Product.
- Quantity received.

Recording a valid restock shall increase the quantity on hand for each affected product.

A restock shall support multiple products in one delivery.

The system shall preserve restock history.

---

## 3.5 Inventory Adjustment Management

The system shall allow authorized users to record inventory adjustments.

An inventory adjustment shall include:

- Product.
- Signed quantity change.
- Adjustment reason.
- Date and time.
- User who recorded the adjustment.
- Optional notes.

Supported adjustment reasons should include at least:

- Damage
- Spoilage
- Shrinkage
- PhysicalCountCorrection
- ManualCorrection
- Other

Recording a valid adjustment shall update the product's quantity on hand by exactly the recorded quantity change.

The system shall preserve adjustment history.

---

## 3.6 Low-Stock Reporting

The system shall allow authorized users to retrieve products that are considered low stock.

A product is considered low stock when:

`QuantityOnHand <= ReorderThreshold`

The low-stock result should provide enough product information for a user to identify what may need replenishment.

---

## 3.7 Inventory History

The system should make it possible to understand why inventory quantities changed over time.

Inventory history should be derived from relevant operational records such as:

- Restock events and restock items.
- Inventory adjustments.

The system should preserve historical transaction records instead of relying only on the current product quantity.

---

## 3.8 Audit Logging

The system shall preserve audit information for important application operations.

Examples include:

- Product created.
- Product updated.
- Product deactivated.
- Category created, updated, or deactivated.
- Vendor created, updated, or deactivated.
- Restock recorded.
- Inventory adjustment recorded.
- Relevant user or role changes.

Audit information should eventually capture sufficient context to answer questions such as:

- Who performed the action?
- What action occurred?
- What business entity was affected?
- When did the action occur?

Audit logging is distinct from inventory history.

Inventory history explains inventory movement.

Audit logging provides broader accountability for important system actions.

---

## 3.9 Authentication and Authorization

The system shall eventually authenticate users.

Authentication answers:

> Who is the user?

The system shall eventually authorize protected operations based on roles or policies.

Authorization answers:

> What is the authenticated user allowed to do?

The initial role model is:

- Employee
- Manager
- Admin

Exact authorization rules may be refined later, but sensitive management and administration operations must not be available to every authenticated user.

---

## 4. Business Rules

## 4.1 Product Rules

- Product name is required.
- SKU is required.
- SKU must be unique.
- Quantity on hand cannot be negative.
- Reorder threshold cannot be negative.
- Cost cannot be negative.
- Retail price cannot be negative.
- A product should normally be deactivated rather than hard deleted when historical records reference it.

---

## 4.2 Category Rules

- Category name is required.
- Categories referenced by products or historical records should generally be deactivated instead of permanently deleted.

---

## 4.3 Vendor Rules

- Vendor name is required.
- Vendors referenced by products or historical records should generally be deactivated instead of permanently deleted.

---

## 4.4 Restock Rules

- A restock must reference a valid vendor.
- A restock must contain at least one line item.
- Each restock line item must reference a valid product.
- Each restock quantity must be greater than zero.
- Recording a restock increases inventory.
- A restock involving multiple products should be treated as one logical business operation.
- Partial completion of a multi-item restock should be avoided; later implementation should preserve atomicity where appropriate.

---

## 4.5 Inventory Adjustment Rules

- An adjustment must reference a valid product.
- QuantityChange cannot be zero.
- Adjustment reason is required.
- Positive and negative adjustments are allowed.
- The resulting quantity on hand cannot be negative.
- The resulting quantity must equal the previous quantity plus the recorded QuantityChange.

---

## 4.6 Historical Integrity Rules

- Important operational history must be preserved.
- Restock records should not disappear because a product or vendor is no longer active.
- Inventory adjustment history should remain available after product deactivation.
- Historical records should not be casually edited or deleted.

---

## 5. Initial Role Capability Matrix

| Capability | Employee | Manager | Admin |
|---|---:|---:|---:|
| View products | Yes | Yes | Yes |
| View inventory quantities | Yes | Yes | Yes |
| View low-stock report | Yes | Yes | Yes |
| Record restock | Yes | Yes | Yes |
| Record inventory adjustment | Limited / Yes | Yes | Yes |
| Create product | No | Yes | Yes |
| Update product | No | Yes | Yes |
| Deactivate product | No | Yes | Yes |
| Manage categories | No | Yes | Yes |
| Manage vendors | No | Yes | Yes |
| Review inventory history | Limited | Yes | Yes |
| Review audit logs | No | Yes | Yes |
| Manage users | No | No | Yes |
| Manage roles | No | No | Yes |

The Employee adjustment and history permissions may be refined when authentication and authorization are designed.

---

## 6. User Stories

### Product Visibility

As an Employee, I want to view products and current inventory quantities so that I can understand what merchandise is available.

### Product Management

As a Manager, I want to create and update products so that the system accurately reflects merchandise the store carries.

### Category Management

As a Manager, I want to organize products by category so that inventory data is easier to manage and query.

### Vendor Management

As a Manager, I want to associate products with vendors so that supplier relationships are documented.

### Restocking

As an Employee, I want to record incoming vendor deliveries so that inventory quantities reflect merchandise received.

### Inventory Corrections

As an Employee, I want to record inventory adjustments with structured reasons so that discrepancies remain traceable.

### Low-Stock Reporting

As a Manager, I want to view products at or below their reorder thresholds so that I can identify merchandise that may need replenishment.

### Inventory History

As a Manager, I want to review inventory changes so that I can understand why stock quantities changed.

### Auditability

As a Manager, I want important application actions recorded so that operational activity is accountable.

### Administration

As an Admin, I want to manage users and roles so that access to sensitive functionality is controlled.

---

## 7. Representative Acceptance Criteria

### 7.1 Create Product

Given valid product data and a unique SKU, when an authorized Manager creates the product, then the product is successfully stored.

Given a duplicate SKU, when an authorized Manager attempts to create a product, then the operation is rejected.

Given a negative retail price, when product creation is attempted, then validation fails.

### 7.2 Record Restock

Given a valid vendor, valid products, and positive received quantities, when an authorized user records a restock, then:

- The restock is stored.
- Its line items are stored.
- The associated inventory quantities increase by the received amounts.

If part of the restock operation fails, the implementation should avoid leaving the system in a partially updated state.

### 7.3 Record Inventory Adjustment

Given a product with QuantityOnHand = 10 and QuantityChange = -3, when a valid adjustment is recorded, then QuantityOnHand becomes 7.

Given a product with QuantityOnHand = 5 and QuantityChange = -6, when the adjustment is attempted, then the operation is rejected because inventory would become negative.

### 7.4 Low-Stock Report

Given a product with QuantityOnHand = 5 and ReorderThreshold = 8, the product shall appear in the low-stock result.

Given a product with QuantityOnHand = 12 and ReorderThreshold = 8, the product shall not appear in the low-stock result.

---

## 8. Non-Functional and Engineering Requirements

The system should be designed so that:

- Business logic is separated from HTTP/controller concerns.
- Core business behavior can be tested independently.
- SQL Server enforces important data integrity rules where appropriate.
- Secrets and production credentials are not committed to Git.
- Configuration can vary by environment.
- The API uses appropriate HTTP methods and status codes.
- The backend can be deployed to Azure.
- Important failures can be diagnosed through logging.
- The architecture remains understandable and maintainable for a single backend application.

The project should remain a modular monolith rather than being prematurely split into microservices.

---

## 9. Explicitly Out of Scope

The following are not MVP requirements:

- POS synchronization
- Customer checkout
- Payment processing
- Fuel pump integration
- Fuel tank management
- Fuel pricing
- Barcode hardware
- Employee payroll
- Employee scheduling
- Accounting
- Purchase-order workflow
- Vendor invoicing
- Multi-store support
- AI forecasting
- Mobile applications
- Microservices
- Kubernetes
- Terraform
- Redis
- Message queues

---

## 10. Requirements-to-Implementation Traceability

These requirements will later drive:

- Domain entities.
- SQL Server schema.
- Primary and foreign keys.
- Entity Framework Core mappings.
- Service-layer business logic.
- REST API endpoints.
- DTO design.
- Validation.
- Authentication and role-based authorization.
- Audit logging.
- Automated tests.
- Azure deployment configuration.

Example:

Requirement:

> Inventory cannot become negative.

Later implementation implications:

- Service-layer validation.
- Controlled inventory mutation.
- Appropriate error response.
- Automated test covering invalid negative-result adjustments.

This requirements document is therefore the authoritative behavioral specification for the MVP unless later project decisions explicitly revise it.
