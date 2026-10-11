-- =========================================================================
-- SCRIPT KHỞI TẠO CƠ SỞ DỮ LIỆU: Coffee_EngagementDB (Engagement / Promotion Service)
-- Thiết kế theo chuẩn DBML dbdiagram.io (UUID / UNIQUEIDENTIFIER)
-- Phụ trách: Trí (Loyalty & Voucher Specialist)
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
        title NVARCHAR(200) NOT NULL,
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
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETDATE()
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
        discount_amount DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        used_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_promo_voucher_usages_vouchers FOREIGN KEY (voucher_id) REFERENCES dbo.promo_vouchers(id) ON DELETE CASCADE
    );
END
GO

-- 3. BẢNG loyalty_tiers (4 Cấp bậc hạng thành viên: Bronze, Silver, Gold, Diamond)
IF OBJECT_ID('dbo.loyalty_tiers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.loyalty_tiers (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        tier_code VARCHAR(20) NOT NULL UNIQUE, -- BRONZE, SILVER, GOLD, DIAMOND
        tier_name NVARCHAR(50) NOT NULL,
        min_points INT NOT NULL DEFAULT 0,
        point_multiplier DECIMAL(4,2) NOT NULL DEFAULT 1.00,
        birthday_discount_percent DECIMAL(5,2) NOT NULL DEFAULT 0.00,
        free_topping_min_order DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        free_topping_count INT NOT NULL DEFAULT 0,
        benefits_description NVARCHAR(MAX) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 4. BẢNG loyalty_members (Hồ sơ tích điểm của thành viên)
IF OBJECT_ID('dbo.loyalty_members', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.loyalty_members (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        user_id UNIQUEIDENTIFIER NOT NULL UNIQUE,  -- Logical Ref -> auth_users.id
        tier_id UNIQUEIDENTIFIER NOT NULL,
        current_points INT NOT NULL DEFAULT 0,
        total_accumulated_points INT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_loyalty_members_tiers FOREIGN KEY (tier_id) REFERENCES dbo.loyalty_tiers(id)
    );
END
GO

-- 5. BẢNG loyalty_point_transactions (Lịch sử cộng/trừ điểm tích lũy)
IF OBJECT_ID('dbo.loyalty_point_transactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.loyalty_point_transactions (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        member_id UNIQUEIDENTIFIER NOT NULL,
        order_id UNIQUEIDENTIFIER NULL,  -- Logical Ref -> order_orders.id
        transaction_type VARCHAR(20) NOT NULL, -- EARN, REDEEM, REFUND, ADJUST
        points_changed INT NOT NULL,
        balance_after INT NOT NULL,
        reason NVARCHAR(500) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_loyalty_point_transactions_members FOREIGN KEY (member_id) REFERENCES dbo.loyalty_members(id) ON DELETE CASCADE
    );
END
GO

