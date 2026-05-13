# 业务系统数据模型设计

**日期**: 2026-05-13
**状态**: Approved

## 概述

为校园干洗店管理系统添加核心业务数据模型（洗衣门店、订单），并实现管理员/普通用户两套不同的页面和功能。替换现有 Dashboard/LaundryShop 页面的硬编码 mock 数据，移除 Clothing 页面。

## 数据模型

### tb_laundry_shop（洗衣门店表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INT IDENTITY PRIMARY KEY | 主键 |
| Name | NVARCHAR(100) NOT NULL | 门店名称 |
| Address | NVARCHAR(200) NOT NULL | 门店地址 |
| ContactPhone | NVARCHAR(20) NOT NULL | 联系电话 |
| ContactPerson | NVARCHAR(50) | 负责人/联系人 |
| Status | NVARCHAR(10) NOT NULL DEFAULT '营业中' | 营业中 / 已停业 |
| BusinessHours | NVARCHAR(50) | 营业时间（如 "08:00-20:00"） |
| Description | NVARCHAR(500) | 门店简介 |
| CreateTime | DATETIME DEFAULT GETDATE() | 创建时间 |

### tb_order（订单表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INT IDENTITY PRIMARY KEY | 主键 |
| OrderNo | NVARCHAR(50) NOT NULL | 订单编号（自动生成） |
| UserId | INT NOT NULL | 关联 tb_user.Id |
| LaundryShopId | INT NOT NULL | 关联 tb_laundry_shop.Id |
| ServiceType | NVARCHAR(20) NOT NULL | 服务类型：洗衣 / 洗鞋 |
| ClothingType | NVARCHAR(50) | 衣物类型（西装、衬衫、羽绒服、外套、裤子、裙子、鞋子） |
| Status | NVARCHAR(20) NOT NULL DEFAULT '待取件' | 订单状态 |
| EstimatedCost | DECIMAL(10,2) NOT NULL DEFAULT 0 | 预估费用 |
| Remark | NVARCHAR(500) | 备注 |
| PickupTime | DATETIME | 预计取件时间 |
| DeliveryTime | DATETIME | 预计送达时间 |
| CreateTime | DATETIME DEFAULT GETDATE() | 创建时间 |

**订单状态流转**: 待取件 → 待清洗 → 洗涤中 → 已完成 → 已送达

## 初始数据

### 洗衣门店种子数据（3-5条）

| Name | Address | ContactPhone | ContactPerson | Status | BusinessHours | Description |
|------|---------|-------------|---------------|--------|---------------|-------------|
| 东校区洗衣店 | 东校区生活区A栋1层 | 010-12345678 | 王师傅 | 营业中 | 08:00-20:00 | 提供干洗、水洗、熨烫等一站式服务 |
| 西校区洗衣店 | 西校区商业街B座2层 | 010-23456789 | 李师傅 | 营业中 | 09:00-19:00 | 专注高端衣物护理与修复 |
| 南校区洗衣店 | 南校区食堂旁 | 010-34567890 | 张师傅 | 营业中 | 08:30-21:00 | 快捷清洗服务，当日可取 |
| 北校区洗衣店 | 北校区体育馆东侧 | 010-45678901 | 赵师傅 | 已停业 | 09:00-18:00 | 正在装修升级中 |

### 权限扩展

新增权限（仅 admin 角色拥有）：

| PermissionKey | PermissionName | Module |
|---------------|----------------|--------|
| manage_users | 用户管理 | user |
| manage_shops | 门店管理 | laundry |
| manage_orders | 订单管理 | order |
| view_orders | 查看我的订单 | order |

新注册用户自动获得 `view_orders` 权限（通过 `user` 角色关联）。

## 角色与导航

### 管理员（admin）侧边栏
- 首页（Dashboard）— 统计数据 + 最近订单
- 用户管理（UserManagement）— 用户增删改查
- 门店管理（LaundryShop）— 门店增删改

### 普通用户侧边栏
- 首页（Dashboard）— 门店卡片列表（网格布局 + 分页）
- 我的订单（MyOrders）— 个人订单列表

### 实现方式
侧边栏在 `_AppLayout.cshtml` 中根据 session 中的 `UserRoles` 判断：
- 若含 `admin` → 显示管理员菜单
- 若含 `user` → 显示普通用户菜单

## 页面设计

### 1. 管理员 - Dashboard
- 统计卡片：注册用户数量（`COUNT(tb_user)`）、洗衣门店数量（`COUNT(tb_laundry_shop)`）、待处理订单数（`WHERE Status IN ('待取件','待清洗','洗涤中')`）、本月营收（`WHERE MONTH(CreateTime)=当前月 AND Status='已送达'` 的 `SUM(EstimatedCost)`）
- 最近订单表格：最近 10 条订单（JOIN 用户表和门店表）

### 2. 管理员 - 用户管理
- 用户列表表格（所有注册用户）
- 新增用户弹窗（表单：用户名、密码、姓名、性别）
- 编辑用户弹窗（表单：姓名、性别、密码）
- 删除确认（二次确认弹窗）
- 搜索/筛选：按用户名或姓名搜索

### 3. 管理员 - 门店管理
- 门店列表表格（从 tb_laundry_shop 查询）
- 新增门店弹窗（表单：名称、地址、联系电话、负责人、营业时间、描述、状态）
- 编辑门店弹窗（同新增）
- 删除确认（二次确认弹窗）
- 状态筛选：全部/营业中/已停业

### 4. 普通用户 - Dashboard（门店展示）
- 门店卡片网格布局：每排 3-4 个卡片（`grid-template-columns: repeat(4, 1fr)`）
- 每个卡片显示：门店名称、地址、联系电话、营业时间、状态
- 分页：每页 8-12 个，底部页码导航
- 已停业门店卡片置灰显示
- 后续可扩展：点击卡片进入下单页面（本期不做）

### 5. 普通用户 - 我的订单
- 订单列表表格：门店名称、服务类型、衣物类型、状态、预估费用、下单时间
- 状态颜色标签（沿用现有 badge-status 样式）
- 按创建时间倒序排列

## 页面生命周期

### 移除
- **Clothing** 页面（`.cshtml` + `.cs`）— 本次需求不再需要
- 侧边栏"衣服管理"导航项

### 替换（mock → 真实数据）
- **Dashboard** — 管理员和普通用户共用同一个页面路径 `/Dashboard`，但根据角色展示不同内容

## 权限更新

更新 RBAC seed data：
- admin 角色增加：`manage_users`, `manage_shops`, `manage_orders`
- user 角色增加：`view_orders`

注册时 `user` 角色自动获得 `view_orders` 权限。

## 约束与注意事项

- 所有表遵循现有约束：`OnModelCreating` 配置，不使用 `[Table]`，`[Key]` 在 Id 上，`CreateTime` 用 `datetime` + `GETDATE()`
- 所有 UI 文本为中文（zh-CN）
- 页面使用现有 `_AppLayout.cshtml` 布局
- 表单操作使用弹窗（popup）方式，参考现有 profile popup 风格
- 删除操作需要二次确认（`confirm()` JavaScript）
- 订单编号生成规则：`ORD-yyyyMMdd-NNNN`
- 订单暂不支持下单流程（本期只做展示和管理），后续可扩展
