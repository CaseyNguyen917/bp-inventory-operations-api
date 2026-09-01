# Synthetic Demo Data Plan

## Purpose

The public portfolio deployment must demonstrate realistic convenience-store workflows without exposing real franchise operational data.

All demo data is synthetic.

## Demo Accounts

Create three demo identities:

| Role | Display Name | Purpose |
|---|---|---|
| Employee | Demo Employee | restocks, adjustments, inventory viewing |
| Manager | Demo Manager | product/category/vendor management and audit viewing |
| Admin | Demo Admin | user/role administration |

Emails come from configuration or stable synthetic values.

Passwords NEVER live in source control and are supplied through local User Secrets / Azure environment configuration.

## Categories

Seed a small realistic catalog:

- Beverages
- Snacks
- Candy
- Tobacco Alternatives / General Convenience only if appropriate for demo data policy
- Automotive
- Household / Essentials

Keep the public demo focused on ordinary convenience-store merchandise. No payment/fuel/customer data.

## Vendors

Use fictional suppliers:

- Metro Beverage Distribution
- Garden State Snacks Supply
- Northstar Convenience Wholesale
- RoadReady Automotive Supply

Vendor contact data must also be fictional.

## Products

Seed approximately 20–30 Products.

Representative examples:

| Product | SKU | Category | Quantity | Threshold |
|---|---|---|---:|---:|
| Coca-Cola 20 oz | COKE20 | Beverages | 28 | 12 |
| Diet Coke 20 oz | DCOKE20 | Beverages | 9 | 12 |
| Sprite 20 oz | SPRITE20 | Beverages | 18 | 10 |
| Bottled Water 20 oz | WATER20 | Beverages | 6 | 12 |
| Monster Energy 16 oz | MONSTER16 | Beverages | 14 | 8 |
| Original Potato Chips | CHIPS-ORG | Snacks | 11 | 10 |
| BBQ Potato Chips | CHIPS-BBQ | Snacks | 5 | 10 |
| Chocolate Bar | CANDY-CHOCO | Candy | 22 | 12 |
| Gum Mint | GUM-MINT | Candy | 7 | 8 |
| Windshield Washer Fluid | AUTO-WWF | Automotive | 4 | 3 |

Include a mixture of:

- healthy stock
- exactly-at-threshold stock
- below-threshold stock

This makes the low-stock endpoint visually demonstrable.

## Prices

Use plausible but clearly synthetic values.

Never claim that demo cost/retail values are actual BP franchise pricing.

## Historical Restocks

Seed approximately 6–10 RestockEvents spread across recent synthetic dates.

Each Restock contains multiple line items.

Purposes:

- demonstrate one-to-many RestockEvent → RestockItem
- show Vendor history
- populate audit/history endpoints
- demonstrate realistic inventory movement

## Inventory Adjustments

Seed approximately 8–12 adjustments using reasons such as:

- Damage
- Spoilage
- Shrinkage
- PhysicalCountCorrection
- ManualCorrection

Mix positive and negative QuantityChange values while preserving non-negative final stock.

## Audit Logs

Do not manually fabricate arbitrary AuditLogs if the application seeding workflow can create them through the same business services.

Preferred approach:

- system/user setup audit entries as appropriate
- demo transactional seed workflows generate realistic audit entries

If the implementation seeds directly through DbContext for simplicity, create coherent synthetic AuditLogs explicitly and document that demo initialization is infrastructure setup rather than normal user activity.

## Deactivated Data

Seed a small number of inactive records:

- one inactive Product
- optionally one inactive Vendor or Category if it does not create confusing relationships

Purpose:

Demonstrate soft deletion and `includeInactive`.

## Determinism

Use stable seed keys:

- Product SKU
- Category name
- Vendor name
- user email

Running seeding repeatedly must not duplicate records.

## Privacy Rule

The public/demo deployment must never contain:

- actual employee names
- actual employee emails
- real passwords
- private supplier contacts
- real sales data
- real payment data
- fuel system data
- customer data

## Demo Story

The seed dataset must make this sequence work immediately:

1. Employee logs in.
2. Employee views low-stock Products.
3. Employee records a Vendor Restock.
4. Product quantity increases.
5. Employee records a damaged-item Adjustment.
6. Quantity decreases.
7. Manager views the resulting history/audit entry.
8. Manager creates or updates a Product.
9. Admin views users and role information.

This is the canonical portfolio demo workflow.
