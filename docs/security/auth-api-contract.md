# Authentication and User Administration API Contract

## 1. Purpose

This document defines the MVP API surface for authentication and internal user administration.

The application uses ASP.NET Core Identity cookie authentication.

There is no public registration endpoint.

---

# 2. Antiforgery Token

## GET /api/auth/antiforgery-token

Anonymous.

Purpose:

Provide the request token needed for unsafe cookie-authenticated API calls.

Representative response:

```json
{
  "requestToken": "<antiforgery-request-token>"
}
```

The server also sets the associated antiforgery cookie.

The client sends the returned value using:

`X-CSRF-TOKEN`

for unsafe requests.

Response:

- `200 OK`

---

# 3. Login

## POST /api/auth/login

Anonymous, but antiforgery-protected.

Request:

```json
{
  "email": "manager@example.com",
  "password": "example-password"
}
```

Do not support `rememberMe` in the MVP.

Successful behavior:

- verify user exists
- verify user is active
- verify password with ASP.NET Core Identity
- apply lockout rules
- issue authentication cookie
- return current-user representation

Response:

- `200 OK`
- `400 Bad Request` for malformed request
- `401 Unauthorized` for failed authentication

Failure messages should be generic.

Do not reveal whether the email address exists.

---

# 4. Current User

## GET /api/auth/me

Authenticated.

Response:

```json
{
  "id": "identity-user-id",
  "email": "manager@example.com",
  "displayName": "Demo Manager",
  "role": "Manager"
}
```

Responses:

- `200 OK`
- `401 Unauthorized`

---

# 5. Logout

## POST /api/auth/logout

Authenticated and antiforgery-protected.

Behavior:

- sign out through Identity
- remove authentication cookie

Response:

- `204 No Content`

---

# 6. Change Password

## POST /api/auth/change-password

Authenticated and antiforgery-protected.

Request:

```json
{
  "currentPassword": "OldPassword!",
  "newPassword": "NewPassword!"
}
```

Behavior:

- verify current password
- validate new password
- change password through UserManager
- update security state
- refresh or reestablish the current sign-in safely
- never log either password

Responses:

- `204 No Content`
- `400 Bad Request` for password-policy/current-password failures
- `401 Unauthorized`

---

# 7. List Users

## GET /api/users

Policy:

`AdminOnly`

Query parameters:

- page
- pageSize
- search
- role
- includeInactive

Response:

`PagedResponse<UserResponse>`

---

# 8. Get User

## GET /api/users/{id}

Policy:

`AdminOnly`

Responses:

- `200 OK`
- `404 Not Found`

---

# 9. Create User

## POST /api/users

Policy:

`AdminOnly`

Antiforgery-protected.

Request:

```json
{
  "email": "employee@example.com",
  "displayName": "Demo Employee",
  "initialPassword": "InitialPassword!",
  "role": "Employee"
}
```

Rules:

- email required and unique
- displayName required
- password must satisfy Identity policy
- role must be Employee, Manager, or Admin
- user begins active
- user receives exactly one business role
- password is passed directly to Identity user creation and never persisted separately

Responses:

- `201 Created`
- `400 Bad Request`
- `409 Conflict` for duplicate email

---

# 10. Change User Role

## PUT /api/users/{id}/role

Policy:

`AdminOnly`

Antiforgery-protected.

Request:

```json
{
  "role": "Manager"
}
```

Rules:

- role must be one of the three known roles
- target user must exist
- replace the user's previous business role
- update security stamp after role change
- do not allow the final active Admin account to lose Admin status

Response:

- `200 OK` with updated UserResponse
- `404 Not Found`
- `409 Conflict` if change would leave the system without an active Admin

---

# 11. Deactivate User

## POST /api/users/{id}/deactivate

Policy:

`AdminOnly`

Antiforgery-protected.

Rules:

- target user must exist
- cannot deactivate an already inactive user as an error; operation should be idempotent
- an Admin must not deactivate the final active Admin
- an Admin should not deactivate their own currently authenticated account through this endpoint
- update Identity security stamp
- write AuditLog

Response:

- `204 No Content`
- `404 Not Found`
- `409 Conflict` for protected Admin/self-deactivation cases

---

# 12. Reactivate User

## POST /api/users/{id}/reactivate

Policy:

`AdminOnly`

Antiforgery-protected.

Rules:

- target user must exist
- set IsActive = true
- update security stamp where appropriate
- write AuditLog

Response:

- `200 OK` with UserResponse
- `404 Not Found`

---

# 13. UserResponse

```json
{
  "id": "identity-user-id",
  "email": "employee@example.com",
  "displayName": "Demo Employee",
  "role": "Employee",
  "isActive": true,
  "createdAtUtc": "2026-08-31T18:30:00Z"
}
```

Never return:

- PasswordHash
- SecurityStamp
- ConcurrencyStamp
- password-reset tokens
- authentication-cookie contents

---

# 14. Explicitly Out of Scope

The MVP does not expose:

- public registration
- forgot-password email workflow
- reset-password email workflow
- email confirmation
- 2FA management
- external OAuth login
- social login
- refresh tokens
- custom JWT issuance
