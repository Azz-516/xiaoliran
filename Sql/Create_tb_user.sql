-- =============================================
-- 创建用户表 tb_user
-- =============================================

CREATE TABLE [dbo].[tb_user] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [Username]   NVARCHAR (50)  NOT NULL,
    [Password]   NVARCHAR (100) NOT NULL,
    [RealName]   NVARCHAR (50)  NOT NULL,
    [Gender]     NVARCHAR (10)  NOT NULL DEFAULT (N'男'),
    [CreateTime] DATETIME       NOT NULL DEFAULT (GETDATE()),
    CONSTRAINT [PK_tb_user_Id] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UK_tb_user_Username] UNIQUE NONCLUSTERED ([Username] ASC)
);
GO

-- =============================================
-- 示例插入（可选，用于测试）
-- =============================================
-- INSERT INTO [dbo].[tb_user] ([Username], [Password], [RealName], [Gender])
-- VALUES ('xiaoliran', 'xiaoliran', N'李然', N'女');
