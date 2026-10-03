-- =========================================================================
-- SCRIPT KHỞI TẠO CƠ SỞ DỮ LIỆU: Coffee_ProductDB (Product / Catalog Service)
-- Thiết kế theo chuẩn DBML dbdiagram.io (UUID / UNIQUEIDENTIFIER)
-- =========================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_ProductDB')
BEGIN
    CREATE DATABASE Coffee_ProductDB;
END
GO

USE Coffee_ProductDB;
GO

-- 1. BẢNG product_categories
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

-- 2. BẢNG product_products
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

-- 3. BẢNG product_options (Sizes & Toppings)
IF OBJECT_ID('dbo.product_options', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.product_options (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        product_id UNIQUEIDENTIFIER NOT NULL,
        option_type VARCHAR(20) NOT NULL, -- SIZE, TOPPING, ICE_LEVEL, SUGAR_LEVEL
        name VARCHAR(50) NOT NULL,
        extra_price DECIMAL(12,2) NOT NULL DEFAULT 0.00,
        is_available BIT NOT NULL DEFAULT 1,
        CONSTRAINT FK_product_options_products FOREIGN KEY (product_id) REFERENCES dbo.product_products(id) ON DELETE CASCADE
    );
END
GO
