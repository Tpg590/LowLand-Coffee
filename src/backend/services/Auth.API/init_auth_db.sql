-- =========================================================================
-- SCRIPT KHỞI TẠO CƠ SỞ DỮ LIỆU: Coffee_AuthDB (Auth & User Service)
-- Thiết kế theo chuẩn DBML dbdiagram.io (UUID / UNIQUEIDENTIFIER)
-- =========================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_AuthDB')
BEGIN
    CREATE DATABASE Coffee_AuthDB;
END
GO

USE Coffee_AuthDB;
GO

-- 1. BẢNG auth_roles
IF OBJECT_ID('dbo.auth_roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.auth_roles (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        name VARCHAR(50) NOT NULL UNIQUE,
        description NVARCHAR(MAX) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 2. BẢNG auth_users
IF OBJECT_ID('dbo.auth_users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.auth_users (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        role_id UNIQUEIDENTIFIER NOT NULL,
        email VARCHAR(255) NOT NULL UNIQUE,
        password_hash VARCHAR(255) NOT NULL,
        full_name NVARCHAR(100) NOT NULL,
        phone_number VARCHAR(20) NULL,
        avatar_url NVARCHAR(MAX) NULL,
        address NVARCHAR(MAX) NULL,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_auth_users_roles FOREIGN KEY (role_id) REFERENCES dbo.auth_roles(id)
    );
END
GO

-- 3. BẢNG auth_permissions
IF OBJECT_ID('dbo.auth_permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.auth_permissions (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        code VARCHAR(100) NOT NULL UNIQUE,
        name NVARCHAR(100) NOT NULL,
        description NVARCHAR(MAX) NULL
    );
END
GO

-- 4. BẢNG auth_role_permissions
IF OBJECT_ID('dbo.auth_role_permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.auth_role_permissions (
        role_id UNIQUEIDENTIFIER NOT NULL,
        permission_id UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY (role_id, permission_id),
        CONSTRAINT FK_auth_role_permissions_roles FOREIGN KEY (role_id) REFERENCES dbo.auth_roles(id) ON DELETE CASCADE,
        CONSTRAINT FK_auth_role_permissions_permissions FOREIGN KEY (permission_id) REFERENCES dbo.auth_permissions(id) ON DELETE CASCADE
    );
END
GO
