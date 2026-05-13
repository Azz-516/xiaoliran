# RBAC 权限系统设计

**日期**: 2026-05-13
**状态**: Approved

## 概述

为校园干洗店管理系统添加基于角色的访问控制（RBAC）系统，用于控制用户登录后各功能页面的访问权限。侧边栏菜单根据用户权限动态显示/隐藏，无权限页面自动跳转并提示。

## 数据模型

### tb_role（角色表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INT IDENTITY PRIMARY KEY | 主键 |
| RoleKey | NVARCHAR(50) NOT NULL UNIQUE | 角色唯一标识（如 `admin`, `user`） |
| RoleName | NVARCHAR(50) NOT NULL | 角色显示名称（如 `管理员`, `普通用户`） |
| Description | NVARCHAR(200) | 角色描述 |
| CreateTime | DATETIME DEFAULT GETDATE() | 创建时间 |

### tb_user_role（用户角色关联表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INT IDENTITY PRIMARY KEY | 主键 |
| UserId | INT NOT NULL | 关联 tb_user.Id |
| RoleId | INT NOT NULL | 关联 tb_role.Id |
| CreateTime | DATETIME DEFAULT GETDATE() | 创建时间 |

### tb_permission（权限表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INT IDENTITY PRIMARY KEY | 主键 |
| PermissionKey | NVARCHAR(100) NOT NULL UNIQUE | 权限唯一标识（如 `view_dashboard`） |
| PermissionName | NVARCHAR(100) NOT NULL | 权限显示名称 |
| Module | NVARCHAR(50) | 所属模块 |
| CreateTime | DATETIME DEFAULT GETDATE() | 创建时间 |

### tb_role_permission（角色权限关联表）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | INT IDENTITY PRIMARY KEY | 主键 |
| RoleId | INT NOT NULL | 关联 tb_role.Id |
| PermissionId | INT NOT NULL | 关联 tb_permission.Id |
| CreateTime | DATETIME DEFAULT GETDATE() | 创建时间 |

## 初始数据

### 角色

| RoleKey | RoleName | Description |
|---------|----------|-------------|
| admin | 管理员 | 拥有全部权限 |
| user | 普通用户 | 基础查看权限 |

### 权限

| PermissionKey | PermissionName | Module |
|---------------|----------------|--------|
| view_dashboard | 查看首页 | system |
| view_laundryshop | 查看洗衣店管理 | laundry |
| view_clothing | 查看衣服管理 | clothing |

### 角色-权限分配

| 角色 | 权限 |
|------|------|
| admin | view_dashboard, view_laundryshop, view_clothing |
| user | view_dashboard, view_clothing |

### 用户-角色分配

新注册用户默认分配 `user` 角色。管理员角色需手动分配（通过数据库或后续管理后台）。

## 技术实现

### 1. Model 与 DbContext

- 新增 4 个模型：`Role`, `UserRole`, `Permission`, `RolePermission`（Models/ 目录）
- 遵循项目约束：使用 `[Key]` 属性，不使用 `[Table]` 属性
- `AppDbContext.OnModelCreating` 中配置表映射、列类型、默认值
- `AppDbContext` 新增 4 个 `DbSet<T>`

### 2. 登录流程改造（Pages/Login.cshtml.cs）

- 用户密码校验通过后，通过 JOIN 查询获取用户的所有权限 key
- 将 `UserRoles`（角色 key 逗号分隔）和 `UserPermissions`（权限 key 逗号分隔）写入 session
- 格式：`admin,user` 和 `view_dashboard,view_clothing`

### 3. 侧边栏权限控制（Pages/Shared/_AppLayout.cshtml）

- 从 session 读取 `UserPermissions`
- 每个 `<a class="nav-item">` 用 `@if` 判断对应权限：

```csharp
@if(Context.Session.GetString("UserPermissions")?.Contains("view_laundryshop") == true) {
    <a class="nav-item" asp-page="/LaundryShop">洗衣店管理</a>
}
```

### 4. 权限校验基类

- 新建 `PermissionPageModel : PageModel`
- 每个需要权限校验的页面 Model 继承此基类
- 基类提供 `RequirePermission(string permissionKey)` 方法，在 `OnPageHandlerSelection` 或 `OnPageHandlerExecuting` 中自动检查
- 无权限时 redirect 到 /Dashboard 并显示 toast 提示

### 5. 注册流程改造（Pages/Register.cshtml.cs + Program.cs /api/register）

- 注册成功后自动为新用户分配 `user` 角色（`tb_user_role` 插入记录）
- 两个注册路径都需要分配默认角色

### 6. 无权限提示

- redirect 时通过 QueryString 传递提示：`/Dashboard?toast=无权限访问`
- Dashboard 页面检测参数后显示 toast（已有 toast 基础架构可复用）

## 约束与注意事项

- 所有表使用 `OnModelCreating` 配置映射，不使用 `[Table]` 属性
- `CreateTime` 统一使用 `datetime` 类型 + `GETDATE()` 默认值
- 不使用 EF Migrations，依赖 `EnsureCreated()` 自动建表
- 密码仍为明文存储（RBAC 不改密码策略）
- 所有 UI 文本为中文（zh-CN）
- 外键约束通过 EF 配置，不使用 `[ForeignKey]` 属性（如有兼容性问题则在 OnModelCreating 中配置）
