# Business Scope

## Project

BP Franchise Inventory & Operations Management System

## Purpose

The system is a single-location back-office application for managing convenience-store merchandise and inventory-related operations for a standard gas station convenience store.

The project is business-informed but intentionally generalized so that it is not tightly coupled to one exact franchise location.

## Primary Business Goals

- Maintain accurate product and inventory information.
- Record inventory increases from vendor deliveries.
- Record inventory changes caused by damage, shrinkage, spoilage, and physical count corrections.
- Identify low-stock products.
- Preserve historical traceability for important inventory operations.
- Support role-based access for employees, managers, and administrators.
- Provide a realistic backend system suitable for deployment to Azure.

## MVP Features

- Product management
- Category management
- Vendor management
- Inventory quantity tracking
- Restock events
- Inventory adjustments
- Low-stock reporting
- Users and roles
- Audit logging
- Seed/demo data
- Azure deployment

## Core Actors

### Employee

Operational user who can view inventory, record restocks, record permitted inventory adjustments, and view low-stock products.

### Manager

Operational administrator who can perform employee actions and manage products, categories, vendors, inventory history, and audit information.

### Admin

Application administrator responsible primarily for users, roles, and privileged access management.

## Key Domain Assumptions

- The application models a single store location.
- Fuel inventory and fuel pump systems are outside the project scope.
- POS/register integration is outside the MVP.
- Inventory sold through the register will not automatically decrement stock.
- Periodic inventory corrections can be represented through inventory adjustments.
- Each product has one primary vendor in the MVP.
- Products, categories, and vendors should generally be deactivated instead of permanently deleted when historical records reference them.
- Inventory changes should normally occur through explicit business transactions rather than arbitrary direct edits.

## Inventory Model

Inventory changes are modeled as business events.

### Restock Event

Represents merchandise received from a vendor.

A restock may contain multiple products. Recording the restock increases the affected product quantities.

### Inventory Adjustment

Represents a manual inventory change caused by events such as:

- Damage
- Spoilage
- Shrinkage
- Physical count correction
- Manual correction
- Other

Inventory adjustments preserve the reason and history behind quantity changes.

## Out of Scope

- POS/register integration
- Customer checkout
- Payment processing
- Fuel pump integration
- Fuel tank management
- Fuel pricing systems
- Barcode hardware
- Payroll
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

## Scope Philosophy

The MVP prioritizes a working backend core, realistic inventory workflows, clean relational database design, REST APIs, Entity Framework Core, authentication and role-based access, Azure deployment, and professional documentation.

Advanced integrations and infrastructure are intentionally excluded unless they become clearly necessary later.
