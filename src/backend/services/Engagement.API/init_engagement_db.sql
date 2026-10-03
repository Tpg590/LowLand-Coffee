-- =========================================================================
-- SCRIPT KHỞI TẠO CƠ SỞ DỮ LIỆU: Coffee_EngagementDB (Engagement / Promotion Service)
-- Thiết kế theo chuẩn DBML dbdiagram.io (UUID / UNIQUEIDENTIFIER)
-- =========================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_EngagementDB')
BEGIN
    CREATE DATABASE Coffee_EngagementDB;
END
GO

USE Coffee_EngagementDB;
GO

-- 1. BẢNG promo_vouchers
IF OBJECT_ID('dbo.promo_vouchers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.promo_vouchers (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        code VARCHAR(50) NOT NULL UNIQUE,
        description NVARCHAR(MAX) NULL,
        discount_type VARCHAR(20) NOT NULL, -- PERCENTAGE hoặc FIXED_AMOUNT
        discount_value DECIMAL(12,2) NOT NULL,
        max_discount_amount DECIMAL(12,2) NULL,
        min_order_value DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        total_quantity INT NOT NULL,
        used_quantity INT NOT NULL DEFAULT 0,
        start_date DATETIME2 NOT NULL,
        end_date DATETIME2 NOT NULL,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 2. BẢNG promo_voucher_usages
IF OBJECT_ID('dbo.promo_voucher_usages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.promo_voucher_usages (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        voucher_id UNIQUEIDENTIFIER NOT NULL,
        user_id UNIQUEIDENTIFIER NOT NULL,  -- Logical Ref -> auth_users.id
        order_id UNIQUEIDENTIFIER NOT NULL, -- Logical Ref -> order_orders.id
        used_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_promo_voucher_usages_vouchers FOREIGN KEY (voucher_id) REFERENCES dbo.promo_vouchers(id) ON DELETE CASCADE
    );
END
GO
