# Authorization Matrix

## 1. Policy Definitions

### EmployeeOrAbove

Allowed roles:

- Employee
- Manager
- Admin

### ManagerOrAbove

Allowed roles:

- Manager
- Admin

### AdminOnly

Allowed role:

- Admin

There is no implicit role hierarchy in ASP.NET Core Identity. These policies explicitly express the hierarchy required by the business.

---

## 2. Authentication Endpoints

| Method | Route | Access |
|---|---|---|
| GET | /api/auth/antiforgery-token | Anonymous |
| POST | /api/auth/login | Anonymous + CSRF token |
| GET | /api/auth/me | Authenticated |
| POST | /api/auth/logout | Authenticated |
| POST | /api/auth/change-password | Authenticated |

---

## 3. Product Endpoints

| Method | Route | Policy |
|---|---|---|
| GET | /api/products | EmployeeOrAbove |
| GET | /api/products/{id} | EmployeeOrAbove |
| GET | /api/products/low-stock | EmployeeOrAbove |
| POST | /api/products | ManagerOrAbove |
| PUT | /api/products/{id} | ManagerOrAbove |
| DELETE | /api/products/{id} | ManagerOrAbove |
| POST | /api/products/{id}/reactivate | ManagerOrAbove |

---

## 4. Category Endpoints

| Method | Route | Policy |
|---|---|---|
| GET | /api/categories | EmployeeOrAbove |
| GET | /api/categories/{id} | EmployeeOrAbove |
| POST | /api/categories | ManagerOrAbove |
| PUT | /api/categories/{id} | ManagerOrAbove |
| DELETE | /api/categories/{id} | ManagerOrAbove |
| POST | /api/categories/{id}/reactivate | ManagerOrAbove |

---

## 5. Vendor Endpoints

| Method | Route | Policy |
|---|---|---|
| GET | /api/vendors | EmployeeOrAbove |
| GET | /api/vendors/{id} | EmployeeOrAbove |
| POST | /api/vendors | ManagerOrAbove |
| PUT | /api/vendors/{id} | ManagerOrAbove |
| DELETE | /api/vendors/{id} | ManagerOrAbove |
| POST | /api/vendors/{id}/reactivate | ManagerOrAbove |

---

## 6. Restock Endpoints

| Method | Route | Policy |
|---|---|---|
| GET | /api/restocks | EmployeeOrAbove |
| GET | /api/restocks/{id} | EmployeeOrAbove |
| POST | /api/restocks | EmployeeOrAbove |

Employees are operational users and are allowed to record normal incoming deliveries.

---

## 7. Inventory Adjustment Endpoints

| Method | Route | Policy |
|---|---|---|
| GET | /api/inventory-adjustments | EmployeeOrAbove |
| GET | /api/inventory-adjustments/{id} | EmployeeOrAbove |
| POST | /api/inventory-adjustments | EmployeeOrAbove |

The MVP allows Employees to record adjustments because damage and physical-count discrepancies are normal operational workflows.

Every adjustment remains historically recorded and attributable to the authenticated user.

A future system could introduce narrower adjustment permissions or manager approval thresholds.

---

## 8. Audit Endpoints

| Method | Route | Policy |
|---|---|---|
| GET | /api/audit-logs | ManagerOrAbove |
| GET | /api/audit-logs/{id} | ManagerOrAbove |

Employees should not have broad access to accountability/security records.

---

## 9. User Administration

| Method | Route | Policy |
|---|---|---|
| GET | /api/users | AdminOnly |
| GET | /api/users/{id} | AdminOnly |
| POST | /api/users | AdminOnly |
| PUT | /api/users/{id}/role | AdminOnly |
| POST | /api/users/{id}/deactivate | AdminOnly |
| POST | /api/users/{id}/reactivate | AdminOnly |

---

## 10. Infrastructure

| Method | Route | Access |
|---|---|---|
| GET | /health | Anonymous |

The health endpoint should expose only minimal health state and no sensitive configuration or diagnostic secrets.

---

## 11. Authorization Principle

Frontend visibility is not security.

Even if a future UI hides a Manager button from Employees, the API must independently enforce ManagerOrAbove.

The server is the authorization boundary.
