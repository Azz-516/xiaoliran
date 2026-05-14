# 订单预估费用 - 定价方案设计

Date: 2026-05-15

## Problem

当前下单表单中"预估费用"为手动输入，缺乏统一计价标准。需要将费用计算从用户输入改为系统自动评估。

## Design Overview

**定价维度：** 服务类型 × 衣物类别 × 数量

**方案：** 基础单价（无阶梯折扣）

## Pricing Matrix

| 衣物类别 | 洗衣 | 干洗 | 熨烫 | 洗鞋 |
|---------|------|------|------|------|
| T恤/衬衫 | ¥15 | ¥25 | ¥12 | — |
| 裤子 | ¥15 | ¥25 | ¥12 | — |
| 外套 | ¥20 | ¥35 | ¥18 | — |
| 鞋子 | — | — | — | ¥20 |
| 其他 | ¥18 | ¥30 | ¥15 | ¥25 |

## Form Changes

### Before
- 服务类型：下拉（洗衣/干洗/熨烫/洗鞋）
- 衣物类型：自由文本输入
- 预估费用：手动输入数字

### After
- 服务类型：下拉（不变）
- 衣物类型：下拉选择（T恤/衬衫 / 裤子 / 外套 / 鞋子 / 其他）
- 数量：下拉/数字输入（1-10件），默认1
- 预估费用：只读显示，自动计算（单价 × 数量）
- 不支持的组合：显示提示 "此服务不支持此类衣物"，禁止提交

## Implementation

### Files to change
1. **Dashboard.cshtml** — 修改下单表单：衣物类型改为下拉、新增数量字段、预估费用改为只读显示
2. **Dashboard.cshtml.cs** — 新增 `OnPostEstimateCost` handler 返回价格，或在前端 JS 中硬编码定价表（推荐后者，纯前端计算无需后端交互）

### Approach
定价表放在前端 JavaScript 中（`site.js` 或 Dashboard 的 inline script），理由：
- 纯展示性估算，不影响订单持久化
- 减少一次网络请求，体验更好
- 当前无管理员修改价格需求

### JS Pricing Object
```js
var PRICING = {
  '洗衣': { 'T恤/衬衫': 15, '裤子': 15, '外套': 20, '鞋子': null, '其他': 18 },
  '干洗': { 'T恤/衬衫': 25, '裤子': 25, '外套': 35, '鞋子': null, '其他': 30 },
  '熨烫': { 'T恤/衬衫': 12, '裤子': 12, '外套': 18, '鞋子': null, '其他': 15 },
  '洗鞋': { 'T恤/衬衫': null, '裤子': null, '外套': null, '鞋子': 20, '其他': 25 }
};
```

### JS Calculation Logic
```js
function calcEstimatedCost() {
  var service = form.ServiceType.value;
  var clothing = form.ClothingType.value;
  var qty = parseInt(form.Quantity.value) || 1;
  var unit = PRICING[service]?.[clothing];
  if (!unit) {
    show unsupported message, disable submit
    return;
  }
  estimatedCostInput.value = unit * qty;
}
```

### Events
- 服务类型 change → recalculate
- 衣物类型 change → recalculate
- 数量 change → recalculate
- showOrderModal() → reset form, recalculate

## Notes
- 后端 `OnPostAddOrder` 中 `EstimatedCost` 字段验证保留，但表单不再有手动输入
- 定价矩阵未来如需可迁移到数据库，当前硬编码足够
