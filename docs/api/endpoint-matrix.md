# Endpoint Matrix

## Product

| Method | Route | Purpose | Expected Role | Success |
|---|---|---|---|---|
| GET | /api/products | List/filter/search Products | Employee+ | 200 |
| GET | /api/products/{id} | Get Product | Employee+ | 200 |
| POST | /api/products | Create Product | Manager+ | 201 |
| PUT | /api/products/{id} | Update Product metadata | Manager+ | 200 |
| DELETE | /api/products/{id} | Soft-deactivate Product | Manager+ | 204 |
| POST | /api/products/{id}/reactivate | Reactivate Product | Manager+ | 200 |
| GET | /api/products/low-stock | Low-stock report | Employee+ | 200 |

## Category

| Method | Route | Purpose | Expected Role | Success |
|---|---|---|---|---|
| GET | /api/categories | List Categories | Employee+ | 200 |
| GET | /api/categories/{id} | Get Category | Employee+ | 200 |
| POST | /api/categories | Create Category | Manager+ | 201 |
| PUT | /api/categories/{id} | Update Category | Manager+ | 200 |
| DELETE | /api/categories/{id} | Soft-deactivate Category | Manager+ | 204 |
| POST | /api/categories/{id}/reactivate | Reactivate Category | Manager+ | 200 |

## Vendor

| Method | Route | Purpose | Expected Role | Success |
|---|---|---|---|---|
| GET | /api/vendors | List Vendors | Employee+ | 200 |
| GET | /api/vendors/{id} | Get Vendor | Employee+ | 200 |
| POST | /api/vendors | Create Vendor | Manager+ | 201 |
| PUT | /api/vendors/{id} | Update Vendor | Manager+ | 200 |
| DELETE | /api/vendors/{id} | Soft-deactivate Vendor | Manager+ | 204 |
| POST | /api/vendors/{id}/reactivate | Reactivate Vendor | Manager+ | 200 |

## Restock

| Method | Route | Purpose | Expected Role | Success |
|---|---|---|---|---|
| GET | /api/restocks | List/filter Restock history | Employee+ | 200 |
| GET | /api/restocks/{id} | Get Restock details | Employee+ | 200 |
| POST | /api/restocks | Record delivery | Employee+ | 201 |

## Inventory Adjustment

| Method | Route | Purpose | Expected Role | Success |
|---|---|---|---|---|
| GET | /api/inventory-adjustments | List/filter adjustments | Employee+/policy | 200 |
| GET | /api/inventory-adjustments/{id} | Get adjustment | Employee+/policy | 200 |
| POST | /api/inventory-adjustments | Record adjustment | Employee+/policy | 201 |

## Audit

| Method | Route | Purpose | Expected Role | Success |
|---|---|---|---|---|
| GET | /api/audit-logs | Search audit history | Manager+ | 200 |
| GET | /api/audit-logs/{id} | Get audit entry | Manager+ | 200 |

## Infrastructure

| Method | Route | Purpose | Authentication | Success |
|---|---|---|---|---|
| GET | /health | Application health probe | No | 200 |

## Deferred

Authentication, current-user, user-management, and role-management routes will be finalized during the Authentication/RBAC design phase.
