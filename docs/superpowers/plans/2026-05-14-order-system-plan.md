# Order System & UI Improvements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace native confirm dialogs with custom confirm, make edit modals close instantly on success, add order placement functionality for regular users, and enhance my orders detail page.

**Architecture:** All changes follow existing AJAX CRUD patterns — `[IgnoreAntiforgeryToken]` on page models, `X-Requested-With: XMLHttpRequest` headers, and `{ success, message }` JSON responses. Order placement adds a new handler to the existing Dashboard page model.

**Tech Stack:** ASP.NET Core Razor Pages, jQuery, Fetch API, Entity Framework Core, SQL Server LocalDB

---

## File Map

| File | Responsibility |
|------|----------------|
| `Pages/UserManagement.cshtml` | Replace `confirm()` → `showConfirmDialog()`, instant modal close on edit |
| `Pages/LaundryShop.cshtml` | Replace `confirm()` → `showConfirmDialog()`, instant modal close on edit |
| `Pages/Orders.cshtml` | Replace `confirm()` → `showConfirmDialog()` |
| `Pages/Dashboard.cshtml` | Add order placement modal + click handler on "立即下单" button |
| `Pages/Dashboard.cshtml.cs` | Add `OnPostAddOrder` handler for order creation |
| `Pages/MyOrders.cshtml` | Add 3 detail columns: 备注, 取件时间, 送达时间 |
| `Pages/MyOrders.cshtml.cs` | Add `Remark`, `PickupTime`, `DeliveryTime` to ViewModel |

---

### Task 1: Custom Confirm Dialog for Delete (Req 1)

**Files:**
- Modify: `Pages/UserManagement.cshtml:195-203`
- Modify: `Pages/LaundryShop.cshtml:253-261`
- Modify: `Pages/Orders.cshtml:180-187`

- [ ] **Step 1: Replace confirm in UserManagement.cshtml**

Find the `confirmDelete` function (line ~195) and replace:

```js
function confirmDelete(id, username) {
    showConfirmDialog('确定要删除用户 "' + username + '" 吗？此操作不可恢复。', function() {
        fetch('?handler=Delete&id=' + id, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function(r) { return r.json(); }).then(function(data) {
                showToast(data.success ? (data.message || '删除成功') : data.message, data.success);
                if (data.success) { location.reload(); }
            });
    });
}
```

- [ ] **Step 2: Replace confirm in LaundryShop.cshtml**

Find the `confirmDelete` function (line ~253) and replace:

```js
function confirmDelete(id, name) {
    showConfirmDialog('确定要删除门店 "' + name + '" 吗？此操作不可恢复。', function() {
        fetch('?handler=Delete&id=' + id, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function(r) { return r.json(); }).then(function(data) {
                showToast(data.success ? (data.message || '删除成功') : data.message, data.success);
                if (data.success) { location.reload(); }
            });
    });
}
```

- [ ] **Step 3: Replace confirm in Orders.cshtml**

Find the `confirmDelete` function (line ~180) and replace:

```js
function confirmDelete(id, orderNo) {
    showConfirmDialog('确定要删除订单 "' + orderNo + '" 吗？此操作不可恢复。', function() {
        fetch('?handler=Delete&id=' + id, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function(r) { return r.json(); }).then(function(data) {
                showToast(data.success ? (data.message || '删除成功') : data.message, data.success);
                if (data.success) { location.reload(); }
            });
    });
}
```

- [ ] **Step 4: Build and verify no compile errors**

Run: `dotnet build D:/Project/xiaoliran/xiaoliran/xiaoliran.csproj`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Pages/UserManagement.cshtml Pages/LaundryShop.cshtml Pages/Orders.cshtml
git commit -m "feat: replace native confirm with custom confirm dialog for delete actions"
```

---

### Task 2: Instant Modal Close on Edit (Req 2)

**Files:**
- Modify: `Pages/UserManagement.cshtml:186-193` (edit form handler)
- Modify: `Pages/LaundryShop.cshtml:240-251` (edit form handler)

- [ ] **Step 1: Update edit handler in UserManagement.cshtml**

Find the edit form submit handler (line ~186) and change the success branch to close modal immediately:

```js
document.getElementById('editForm').addEventListener('submit', function(e) {
    e.preventDefault();
    var fd = new FormData(this);
    fetch('?handler=Edit', { method: 'POST', body: fd, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(function(r) { return r.json(); }).then(function(data) {
            showToast(data.success ? (data.message || '编辑成功') : data.message, data.success);
            if (data.success) { closeEditModal(); location.reload(); }
        });
});
```

- [ ] **Step 2: Update edit handler in LaundryShop.cshtml**

Find the edit form submit handler (line ~240) and change the success branch to close modal immediately:

```js
document.getElementById('editForm').addEventListener('submit', function(e) {
    e.preventDefault();
    if (!validateTime('editBusinessHoursStart', 'editBusinessHoursEnd')) return;
    var fd = new FormData(this);
    fd.set('BusinessHours', buildBusinessHours('editBusinessHoursStart', 'editBusinessHoursEnd'));
    fd.delete('BusinessHoursStart');
    fd.delete('BusinessHoursEnd');
    fetch('?handler=Edit', { method: 'POST', body: fd, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
        .then(function(r) { return r.json(); }).then(function(data) {
            showToast(data.success ? (data.message || '编辑成功') : data.message, data.success);
            if (data.success) { closeEditModal(); location.reload(); }
        });
});
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build D:/Project/xiaoliran/xiaoliran/xiaoliran.csproj`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add Pages/UserManagement.cshtml Pages/LaundryShop.cshtml
git commit -m "fix: close edit modal instantly on success instead of 1.5s delay"
```

---

### Task 3: Order Placement Modal & Handler (Req 3.1)

**Files:**
- Modify: `Pages/Dashboard.cshtml` — add order modal, wire up "立即下单" button
- Modify: `Pages/Dashboard.cshtml.cs` — add `[IgnoreAntiforgeryToken]` + `OnPostAddOrder` handler

- [ ] **Step 1: Update DashboardModel.cs**

Add `[IgnoreAntiforgeryToken]` attribute and `OnPostAddOrder` handler:

```csharp
using Microsoft.AspNetCore.Mvc;
// ... existing using statements ...

namespace xiaoliran.Pages
{
    [IgnoreAntiforgeryToken]
    public class DashboardModel : PageModel
    {
        // ... existing code ...

        public async Task<IActionResult> OnPostAddOrder()
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                    return new JsonResult(new { success = false, message = "未登录" });

                var laundryShopId = int.Parse(Request.Form["LaundryShopId"]);
                var serviceType = Request.Form["ServiceType"].ToString();
                var clothingType = Request.Form["ClothingType"].ToString();
                var remark = Request.Form["Remark"].ToString();
                var estimatedCostStr = Request.Form["EstimatedCost"].ToString();
                var pickupTimeStr = Request.Form["PickupTime"].ToString();

                if (string.IsNullOrWhiteSpace(clothingType))
                    return new JsonResult(new { success = false, message = "请输入衣物类型" });

                if (!decimal.TryParse(estimatedCostStr, out var estimatedCost) || estimatedCost < 0)
                    return new JsonResult(new { success = false, message = "请输入有效的预估费用" });

                var orderNo = $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}";

                var order = new Order
                {
                    OrderNo = orderNo,
                    UserId = int.Parse(userId),
                    LaundryShopId = laundryShopId,
                    ServiceType = serviceType,
                    ClothingType = clothingType,
                    Status = "待取件",
                    EstimatedCost = estimatedCost,
                    Remark = remark,
                    PickupTime = string.IsNullOrWhiteSpace(pickupTimeStr) ? null : DateTime.Parse(pickupTimeStr)
                };

                _db.Orders.Add(order);
                await _db.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "下单成功", orderNo = orderNo });
            }
            catch (Exception)
            {
                return new JsonResult(new { success = false, message = "服务异常，请稍后重试" });
            }
        }
    }
}
```

- [ ] **Step 2: Update Dashboard.cshtml — wire up "立即下单" button**

Find the button (line ~154):
```html
<button class="btn-shop-action">立即下单</button>
```
Replace with:
```html
<button class="btn-shop-action" onclick="showOrderModal(@shop.Id, '@shop.Name.Replace("'", "\\'")')">立即下单</button>
```

- [ ] **Step 3: Update Dashboard.cshtml — add order modal HTML**

Add AFTER the closing `}` of the `@if (Model.IsAdmin) ... else ...` block (after line 176) and BEFORE the `@if (!string.IsNullOrEmpty(...))` block (before line 178):

```html
<!-- Order Modal -->
<div class="modal-overlay" id="orderModal">
    <div class="modal-content" style="max-width:560px;">
        <h3>下单</h3>
        <form id="orderForm">
            <input type="hidden" name="LaundryShopId" id="orderShopId" />
            <div class="form-row">
                <label>洗衣店</label>
                <input type="text" id="orderShopName" class="form-readonly" readonly />
            </div>
            <div class="form-row">
                <label>服务类型</label>
                <select name="ServiceType" class="form-input">
                    <option value="洗衣">洗衣</option>
                    <option value="干洗">干洗</option>
                    <option value="熨烫">熨烫</option>
                    <option value="洗鞋">洗鞋</option>
                </select>
            </div>
            <div class="form-row">
                <label>衣物类型</label>
                <input type="text" name="ClothingType" class="form-input" placeholder="如：衬衫、外套" required />
            </div>
            <div class="form-row">
                <label>备注</label>
                <textarea name="Remark" class="form-input" rows="2" placeholder="选填"></textarea>
            </div>
            <div class="form-row">
                <label>取件时间</label>
                <input type="datetime-local" name="PickupTime" class="form-input" />
            </div>
            <div class="form-row">
                <label>预估费用（元）</label>
                <input type="number" name="EstimatedCost" class="form-input" step="0.01" min="0" placeholder="0.00" required />
            </div>
            <div class="modal-footer">
                <button type="button" class="btn-secondary-app" onclick="closeOrderModal()">取消</button>
                <button type="submit" class="btn-primary-app">确认下单</button>
            </div>
        </form>
    </div>
</div>
```

- [ ] **Step 4: Add order modal JS to Dashboard.cshtml**

Add after step 3's content, wrapping ALL scripts (including the existing toast message) in a `@section Scripts`:

```cshtml
@section Scripts {
<script>
    function showOrderModal(shopId, shopName) {
        document.getElementById('orderShopId').value = shopId;
        document.getElementById('orderShopName').value = shopName;
        document.getElementById('orderModal').classList.add('show');
    }
    function closeOrderModal() {
        document.getElementById('orderModal').classList.remove('show');
        document.getElementById('orderForm').reset();
    }
    document.getElementById('orderForm').addEventListener('submit', function(e) {
        e.preventDefault();
        var fd = new FormData(this);
        fetch('?handler=AddOrder', { method: 'POST', body: fd, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function(r) { return r.json(); }).then(function(data) {
                showToast(data.success ? (data.message || '下单成功') : data.message, data.success);
                if (data.success) { closeOrderModal(); setTimeout(function() { location.reload(); }, 1500); }
            });
    });
    document.getElementById('orderModal').addEventListener('click', function(e) { if (e.target === this) closeOrderModal(); });
</script>
@if (!string.IsNullOrEmpty(Model.ToastMessage))
{
    <script>
        document.addEventListener('DOMContentLoaded', function() {
            showToast('@Model.ToastMessage', false);
        });
    </script>
}
}
```

Remove the old inline toast `<script>` block that was at lines 178-185 (the one outside any section).

- [ ] **Step 5: Build and verify**

Run: `dotnet build D:/Project/xiaoliran/xiaoliran/xiaoliran.csproj`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add Pages/Dashboard.cshtml Pages/Dashboard.cshtml.cs
git commit -m "feat: add order placement modal on user dashboard"
```

---

### Task 4: My Orders Detail Page Enhancement (Req 3.2)

**Files:**
- Modify: `Pages/MyOrders.cshtml.cs` — add fields to ViewModel, update query
- Modify: `Pages/MyOrders.cshtml` — add 3 columns to table

- [ ] **Step 1: Update MyOrderViewModel**

Add `Remark`, `PickupTime`, `DeliveryTime` properties to `MyOrderViewModel`:

```csharp
public class MyOrderViewModel
{
    public string OrderNo { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string ClothingType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public string CreateTime { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public string PickupTime { get; set; } = string.Empty;
    public string DeliveryTime { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Update query in OnGet**

In the `Select` projection, add the new fields:

```csharp
Orders = _db.Orders
    .Where(o => o.UserId == uid)
    .OrderByDescending(o => o.CreateTime)
    .Select(o => new MyOrderViewModel
    {
        OrderNo = o.OrderNo,
        ShopName = _db.LaundryShops.Where(s => s.Id == o.LaundryShopId).Select(s => s.Name).FirstOrDefault() ?? "-",
        ServiceType = o.ServiceType,
        ClothingType = o.ClothingType ?? "-",
        Status = o.Status,
        EstimatedCost = o.EstimatedCost,
        CreateTime = o.CreateTime.ToString("yyyy-MM-dd HH:mm"),
        Remark = o.Remark ?? "-",
        PickupTime = o.PickupTime.HasValue ? o.PickupTime.Value.ToString("yyyy-MM-dd HH:mm") : "-",
        DeliveryTime = o.DeliveryTime.HasValue ? o.DeliveryTime.Value.ToString("yyyy-MM-dd HH:mm") : "-"
    }).ToList();
```

- [ ] **Step 3: Update MyOrders.cshtml table**

Update the `<thead>` to add 3 columns:

```html
<thead>
    <tr>
        <th>订单号</th>
        <th>门店</th>
        <th>服务类型</th>
        <th>衣物类型</th>
        <th>费用</th>
        <th>状态</th>
        <th>备注</th>
        <th>取件时间</th>
        <th>送达时间</th>
        <th>下单时间</th>
    </tr>
</thead>
```

Update the colspan in the empty row from `7` to `11`:

```html
<tr><td colspan="11" style="text-align:center;color:#a0aec0;">暂无订单</td></tr>
```

Update the `@foreach` row to add 3 cells:

```html
<td>@order.Remark</td>
<td>@order.PickupTime</td>
<td>@order.DeliveryTime</td>
```

Insert these between the status cell and the create time cell.

- [ ] **Step 4: Build and verify**

Run: `dotnet build D:/Project/xiaoliran/xiaoliran/xiaoliran.csproj`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add Pages/MyOrders.cshtml Pages/MyOrders.cshtml.cs
git commit -m "feat: add remark, pickup time, delivery time columns to my orders page"
```

---

### Task 5: Final Build & Smoke Test

**Files:** None (build verification only)

- [ ] **Step 1: Final build**

Run: `dotnet build D:/Project/xiaoliran/xiaoliran/xiaoliran.csproj`
Expected: Build succeeded, 0 warnings

- [ ] **Step 2: Run the app briefly**

Run: `dotnet run --project D:/Project/xiaoliran/xiaoliran/xiaoliran.csproj`
Expected: Server starts on http://localhost:5079

- [ ] **Step 3: Commit**

```bash
git push
```
