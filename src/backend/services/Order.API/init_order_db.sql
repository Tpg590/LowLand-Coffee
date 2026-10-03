-- =========================================================================
-- SCRIPT KHỞI TẠO CƠ SỞ DỮ LIỆU: Coffee_OrderDB (Order & Payment Service)
-- Thiết kế theo chuẩn DBML dbdiagram.io (UUID / UNIQUEIDENTIFIER)
-- =========================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_OrderDB')
BEGIN
    CREATE DATABASE Coffee_OrderDB;
END
GO

USE Coffee_OrderDB;
GO

-- 1. BẢNG order_orders
IF OBJECT_ID('dbo.order_orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.order_orders (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        user_id UNIQUEIDENTIFIER NOT NULL, -- Logical Ref -> auth_users.id
        order_code VARCHAR(20) NOT NULL UNIQUE,
        total_amount DECIMAL(12,2) NOT NULL,
        discount_amount DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        final_amount DECIMAL(12,2) NOT NULL,
        status VARCHAR(30) NOT NULL, -- PENDING, PROCESSING, SHIPPING, COMPLETED, CANCELLED
        cancel_reason NVARCHAR(MAX) NULL,
        note NVARCHAR(MAX) NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        updated_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 2. BẢNG order_items
IF OBJECT_ID('dbo.order_items', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.order_items (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        order_id UNIQUEIDENTIFIER NOT NULL,
        product_id UNIQUEIDENTIFIER NOT NULL, -- Logical Ref -> product_products.id
        product_name VARCHAR(150) NOT NULL,
        unit_price DECIMAL(12,2) NOT NULL,
        quantity INT NOT NULL,
        options_summary NVARCHAR(MAX) NULL, -- 'Size L, Topping Thach Ca Phe'
        subtotal DECIMAL(12,2) NOT NULL,
        CONSTRAINT FK_order_items_orders FOREIGN KEY (order_id) REFERENCES dbo.order_orders(id) ON DELETE CASCADE
    );
END
GO

-- 3. BẢNG order_payments
IF OBJECT_ID('dbo.order_payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.order_payments (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        order_id UNIQUEIDENTIFIER NOT NULL,
        payment_method VARCHAR(30) NOT NULL, -- VNPAY, PAYOS, CASH
        transaction_code VARCHAR(100) NULL,
        amount DECIMAL(12,2) NOT NULL,
        payment_status VARCHAR(30) NOT NULL, -- PENDING, SUCCESS, FAILED
        paid_at DATETIME2 NULL,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_order_payments_orders FOREIGN KEY (order_id) REFERENCES dbo.order_orders(id) ON DELETE CASCADE
    );
END
GO
