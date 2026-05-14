# Design: Order System & UI Improvements

## Overview

Three requirements: (1) replace browser confirm dialog with custom confirm for delete actions, (2) instant modal close on edit success/failure, (3) full order placement and management system.

## Requirement 1: Custom Confirm Dialog for Delete

### Current State
Delete buttons in `UserManagement.cshtml`, `LaundryShop.cshtml`, and `Orders.cshtml` use native `confirm()`. The project already has a custom `showConfirmDialog(message, callback)` in `site.js` and `#confirmDialog` element in `_AppLayout.cshtml`, but it is not wired to any delete buttons.

### Changes
Replace all three `confirmDelete` functions to use `showConfirmDialog` instead of native `confirm`:

```js
// Before
function confirmDelete(id, name) {
    if (confirm('确定要删除 "' + name + '" 吗？此操作不可恢复。')) { ... }
}

// After
function confirmDelete(id, name) {
    showConfirmDialog('确定要删除 "' + name + '" 吗？此操作不可恢复。', function() {
        fetch('?handler=Delete&id=' + id, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function(r) { return r.json(); }).then(function(data) { ... });
    });
}
```

### Files Modified
- `Pages/UserManagement.cshtml` — `confirmDelete` function
- `Pages/LaundryShop.cshtml` — `confirmDelete` function
- `Pages/Orders.cshtml` — `confirmDelete` function

## Requirement 2: Instant Modal Close on Edit

### Current State
Edit forms in `UserManagement.cshtml` and `LaundryShop.cshtml` use `setTimeout(function() { location.reload(); }, 1000)` after a successful save. The modal stays open for 1 second before the page reloads.

### Changes
- Close the edit modal immediately after success toast is shown, then reload the page
- The toast can still be shown before reload; the modal just shouldn't stay open

**UserManagement.cshtml** — edit handler:
```js
// Before
if (data.success) setTimeout(function() { location.reload(); }, 1000);

// After
if (data.success) { closeEditModal(); location.reload(); }
```

**LaundryShop.cshtml** — edit handler:
```js
// Before
if (data.success) setTimeout(function() { location.reload(); }, 1000);

// After
if (data.success) { closeEditModal(); location.reload(); }
```

### Files Modified
- `Pages/UserManagement.cshtml` — edit form submit handler
- `Pages/LaundryShop.cshtml` — edit form submit handler

## Requirement 3: Order System

### 3.1 Order Placement (User → Shop)

**Trigger**: User clicks "立即下单" button on shop card in user Dashboard.

**Order Modal Fields** (added to `Dashboard.cshtml`):
| Field | Type | Required | Default |
|-------|------|----------|---------|
| 洗衣店 | text (readonly) | Yes | Shop name |
| 服务类型 | select | Yes | 洗衣 |
| 衣物类型 | text | Yes | — |
| 备注 | textarea | No | — |
| 取件时间 | datetime-local | No | — |
| 预估费用 | number (2 decimals) | Yes | 0 |

**Order Number Format**: `ORD-YYYYMMDD-XXXX` where XXXX is first 4 chars of a GUID (e.g., `ORD-20260514-A3F2`).

**Submit Handler**: Add to `DashboardModel`:
- `[IgnoreAntiforgeryToken]` attribute (consistent with LaundryShop/UserManagement pattern)
- `OnPostAddOrder` handler — reads form data via `Request.Form`, validates required fields, creates Order entity with status `待取件`, UserId from session, LaundryShopId from form

### 3.2 My Orders Detail Page

**Changes to `MyOrders.cshtml` and `MyOrdersModel`**:
- Add 3 columns to the table: 备注, 取件时间, 送达时间
- Add corresponding fields to `MyOrderViewModel`: `Remark`, `PickupTime`, `DeliveryTime`
- Format datetime fields as `yyyy-MM-dd HH:mm` or display `-` if null

### 3.3 Admin Order Management

**No changes needed**. `Orders.cshtml` and `OrdersModel` already support:
- Status editing via modal (free switch to any status)
- Delete via native confirm (will be changed to custom confirm per Requirement 1)
- Pagination, search by order number, filter by status

### 3.4 Monthly Revenue

**No changes needed**. Dashboard admin view already counts "已送达" orders for monthly revenue.

## Files Summary

| File | Change Type | Requirement |
|------|-------------|-------------|
| `Pages/UserManagement.cshtml` | Edit | Req 1, Req 2 |
| `Pages/LaundryShop.cshtml` | Edit | Req 1, Req 2 |
| `Pages/Orders.cshtml` | Edit | Req 1 |
| `Pages/Dashboard.cshtml` | Edit | Req 3 |
| `Pages/Dashboard.cshtml.cs` | Edit | Req 3 |
| `Pages/MyOrders.cshtml` | Edit | Req 3 |
| `Pages/MyOrders.cshtml.cs` | Edit | Req 3 |

## Error Handling

- Order creation: return `{ success: false, message: "error text" }` for missing required fields, database errors
- Delete: toast error message on failure, reload on success
- Edit: show error toast on failure, modal closes immediately on success
