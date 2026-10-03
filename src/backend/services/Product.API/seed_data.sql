-- =========================================================================
-- SEED DATA: Coffee_ProductDB (Product / Catalog Service)
-- =========================================================================

USE Coffee_ProductDB;
GO

-- 1. UUID Danh mục
DECLARE @CatCoffee UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';
DECLARE @CatTea UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000002';
DECLARE @CatFreeze UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000003';
DECLARE @CatCake UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000004';

IF NOT EXISTS (SELECT 1 FROM dbo.product_categories WHERE id = @CatCoffee)
BEGIN
    INSERT INTO dbo.product_categories (id, name, display_order, is_active, created_at) VALUES
    (@CatCoffee, 'Ca Phe Truyen Thong', 1, 1, GETDATE()),
    (@CatTea, 'Tra Trai Cay & Macchiato', 2, 1, GETDATE()),
    (@CatFreeze, 'Freeze & Da Xay', 3, 1, GETDATE()),
    (@CatCake, 'Banh Ngot & Snack', 4, 1, GETDATE());
END

-- 2. UUID Sản phẩm
DECLARE @ProdPhinSua UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @ProdPhinDen UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @ProdBacXiu UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
DECLARE @ProdTraSen UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';
DECLARE @ProdTraDao UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000005';
DECLARE @ProdFreezeMatcha UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000006';
DECLARE @ProdMousse UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000007';

IF NOT EXISTS (SELECT 1 FROM dbo.product_products WHERE id = @ProdPhinSua)
BEGIN
    INSERT INTO dbo.product_products (id, category_id, name, description, base_price, image_url, is_available, is_deleted, created_at, updated_at) VALUES
    (@ProdPhinSua, @CatCoffee, 'Phin Sua Da LowLand', N'Cà phê phin đậm đà hòa quyện sữa đặc béo ngậy ngọt ngào', 29000.00, 'https://placehold.co/300', 1, 0, GETDATE(), GETDATE()),
    (@ProdPhinDen, @CatCoffee, 'Phin Den Da', N'Hương vị cà phê nguyên chất thanh lịch dành cho người sành gu', 25000.00, 'https://placehold.co/300', 1, 0, GETDATE(), GETDATE()),
    (@ProdBacXiu, @CatCoffee, 'Bac Xiu Sua Tuoi', N'Nhiều sữa ít cà phê, béo thơm dịu nhẹ khởi đầu ngày mới', 32000.00, 'https://placehold.co/300', 1, 0, GETDATE(), GETDATE()),
    (@ProdTraSen, @CatTea, 'Tra Sen Vang Hạt Sen', N'Trà ô long thanh khiết kết hợp hạt sen bùi ngậy và củ năng giòn', 45000.00, 'https://placehold.co/300', 1, 0, GETDATE(), GETDATE()),
    (@ProdTraDao, @CatTea, 'Tra Dao Cam Sa', N'Vị đào mọng nước hòa quyện cam tươi mát và hương sả nồng nàn', 45000.00, 'https://placehold.co/300', 1, 0, GETDATE(), GETDATE()),
    (@ProdFreezeMatcha, @CatFreeze, 'Freeze Tra Xanh Matcha', N'Matcha Nhật Bản xay đá nhuyễn kèm thạch matcha mát lạnh', 49000.00, 'https://placehold.co/300', 1, 0, GETDATE(), GETDATE()),
    (@ProdMousse, @CatCake, 'Banh Mousse Cacao', N'Bánh mousse mềm mịn tan ngay trong miệng với vị đắng ngọt dịu', 35000.00, 'https://placehold.co/300', 1, 0, GETDATE(), GETDATE());

    -- 3. Nạp Product Options (Size & Topping)
    -- Options cho Phin Sữa Đá
    INSERT INTO dbo.product_options (product_id, option_type, name, extra_price, is_available) VALUES
    (@ProdPhinSua, 'SIZE', 'Size S', 0.00, 1),
    (@ProdPhinSua, 'SIZE', 'Size M', 6000.00, 1),
    (@ProdPhinSua, 'SIZE', 'Size L', 12000.00, 1),
    (@ProdPhinSua, 'TOPPING', 'Thach Ca Phe', 10000.00, 1),
    (@ProdPhinSua, 'TOPPING', 'Kem Cheese', 12000.00, 1);

    -- Options cho Trà Sen Vàng
    INSERT INTO dbo.product_options (product_id, option_type, name, extra_price, is_available) VALUES
    (@ProdTraSen, 'SIZE', 'Size S', 0.00, 1),
    (@ProdTraSen, 'SIZE', 'Size M', 6000.00, 1),
    (@ProdTraSen, 'SIZE', 'Size L', 12000.00, 1),
    (@ProdTraSen, 'TOPPING', 'Hat Sen Them', 10000.00, 1),
    (@ProdTraSen, 'TOPPING', 'Tran Chau Trang', 10000.00, 1);

    -- Options cho Freeze Trà Xanh
    INSERT INTO dbo.product_options (product_id, option_type, name, extra_price, is_available) VALUES
    (@ProdFreezeMatcha, 'SIZE', 'Size M', 0.00, 1),
    (@ProdFreezeMatcha, 'SIZE', 'Size L', 10000.00, 1),
    (@ProdFreezeMatcha, 'TOPPING', 'Whipping Cream Them', 10000.00, 1);
END
GO
