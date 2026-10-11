-- ====================================================================================
-- SCRIPT TỔNG HỢP: KHỞI TẠO TOÀN BỘ 5 CƠ SỞ DỮ LIỆU MICROSERVICES (PRN232)
-- Hệ thống: LowLand Coffee Website (Thiết kế theo chuẩn DBML dbdiagram.io - UUIDs)
-- 1. Coffee_AuthDB (Auth & User - Khang)
-- 2. Coffee_ProductDB (Product / Catalog - Phú)
-- 3. Coffee_OrderDB (Order & Payment - Tuấn)
-- 4. Coffee_EngagementDB (Loyalty & Promotion - Trí)
-- 5. Coffee_NotificationDB (Notification & Logs - Thành)
-- ====================================================================================

-- =========================================================================
-- 1. DATABASE: Coffee_AuthDB
-- =========================================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_AuthDB') CREATE DATABASE Coffee_AuthDB;
GO
USE Coffee_AuthDB;
GO

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

IF OBJECT_ID('dbo.auth_role_permissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.auth_role_permissions (
        role_id UNIQUEIDENTIFIER NOT NULL,
        permission_id UNIQUEIDENTIFIER NOT NULL,
        PRIMARY KEY (role_id, permission_id),
        CONSTRAINT FK_auth_rp_roles FOREIGN KEY (role_id) REFERENCES dbo.auth_roles(id) ON DELETE CASCADE,
        CONSTRAINT FK_auth_rp_permissions FOREIGN KEY (permission_id) REFERENCES dbo.auth_permissions(id) ON DELETE CASCADE
    );
END
GO

-- =========================================================================
-- 2. DATABASE: Coffee_ProductDB
-- =========================================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_ProductDB') CREATE DATABASE Coffee_ProductDB;
GO
USE Coffee_ProductDB;
GO

IF OBJECT_ID('dbo.product_categories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.product_categories (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        name VARCHAR(100) NOT NULL,
        display_order INT NOT NULL DEFAULT 0,
        is_active BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF OBJECT_ID('dbo.product_products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.product_products (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        category_id UNIQUEIDENTIFIER NOT NULL,
        name VARCHAR(150) NOT NULL,
        description NVARCHAR(MAX) NULL,
        base_price DECIMAL(12,2) NOT NULL,
        image_url NVARCHAR(MAX) NULL,
        is_available BIT NOT NULL DEFAULT 1,
        is_deleted BIT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_product_products_categories FOREIGN KEY (category_id) REFERENCES dbo.product_categories(id)
    );
END
GO

IF OBJECT_ID('dbo.product_options', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.product_options (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        product_id UNIQUEIDENTIFIER NOT NULL,
        option_type VARCHAR(20) NOT NULL,
        name VARCHAR(50) NOT NULL,
        extra_price DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        is_available BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_product_options_products FOREIGN KEY (product_id) REFERENCES dbo.product_products(id) ON DELETE CASCADE
    );
END
GO

-- =========================================================================
-- 3. DATABASE: Coffee_OrderDB
-- =========================================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_OrderDB') CREATE DATABASE Coffee_OrderDB;
GO
USE Coffee_OrderDB;
GO

IF OBJECT_ID('dbo.order_orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.order_orders (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        user_id UNIQUEIDENTIFIER NOT NULL,
        order_code VARCHAR(20) NOT NULL UNIQUE,
        total_amount DECIMAL(12,2) NOT NULL,
        discount_amount DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        final_amount DECIMAL(12,2) NOT NULL,
        status VARCHAR(30) NOT NULL,
        cancel_reason NVARCHAR(MAX) NULL,
        note NVARCHAR(MAX) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF OBJECT_ID('dbo.order_items', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.order_items (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        order_id UNIQUEIDENTIFIER NOT NULL,
        product_id UNIQUEIDENTIFIER NOT NULL,
        product_name VARCHAR(150) NOT NULL,
        unit_price DECIMAL(12,2) NOT NULL,
        quantity INT NOT NULL,
        options_summary NVARCHAR(MAX) NULL,
        subtotal DECIMAL(12,2) NOT NULL,
        CONSTRAINT FK_order_items_orders FOREIGN KEY (order_id) REFERENCES dbo.order_orders(id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID('dbo.order_payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.order_payments (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        order_id UNIQUEIDENTIFIER NOT NULL,
        payment_method VARCHAR(30) NOT NULL,
        transaction_code VARCHAR(100) NULL,
        amount DECIMAL(12,2) NOT NULL,
        payment_status VARCHAR(30) NOT NULL,
        paid_at DATETIME2 NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_order_payments_orders FOREIGN KEY (order_id) REFERENCES dbo.order_orders(id) ON DELETE CASCADE
    );
END
GO
-- =========================================================================
-- 4. DATABASE: Coffee_EngagementDB (Loyalty & Promotion - Trí)
-- =========================================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_EngagementDB') CREATE DATABASE Coffee_EngagementDB;
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

-- 6. BẢNG engagement_reviews (Đánh giá món nước & phản hồi khách hàng)
IF OBJECT_ID('dbo.engagement_reviews', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.engagement_reviews (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        product_id UNIQUEIDENTIFIER NOT NULL,  -- Logical Ref -> product_products.id
        user_id UNIQUEIDENTIFIER NOT NULL,     -- Logical Ref -> auth_users.id
        order_id UNIQUEIDENTIFIER NULL,        -- Logical Ref -> order_orders.id
        rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
        comment NVARCHAR(MAX) NULL,
        is_approved BIT NOT NULL DEFAULT 1,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- =========================================================================
-- 5. DATABASE: Coffee_NotificationDB
-- =========================================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_NotificationDB') CREATE DATABASE Coffee_NotificationDB;
GO
USE Coffee_NotificationDB;
GO

IF OBJECT_ID('dbo.notif_notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.notif_notifications (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        user_id UNIQUEIDENTIFIER NOT NULL,
        title VARCHAR(200) NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        type VARCHAR(50) NOT NULL,
        reference_id UNIQUEIDENTIFIER NULL,
        is_read BIT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

IF OBJECT_ID('dbo.notif_notification_logs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.notif_notification_logs (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        notification_id UNIQUEIDENTIFIER NULL,
        channel VARCHAR(30) NOT NULL,
        status VARCHAR(20) NOT NULL,
        error_message NVARCHAR(MAX) NULL,
        sent_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_notif_notification_logs_notifications FOREIGN KEY (notification_id) REFERENCES dbo.notif_notifications(id) ON DELETE CASCADE
    );
END
GO
