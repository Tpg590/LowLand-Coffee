-- =========================================================================
-- MASTER SEED DATA SCRIPT: LOWLAND COFFEE MICROSERVICES
-- Khởi tạo toàn bộ dữ liệu mẫu cho 5 cơ sở dữ liệu Microservices (MSSQL / DBML)
-- =========================================================================

-- =========================================================================
-- 1. DATABASE: Coffee_AuthDB (Auth & User Service)
-- =========================================================================
USE Coffee_AuthDB;
GO

DECLARE @AdminRoleId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @StaffRoleId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @CustomerRoleId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

-- Roles
IF NOT EXISTS (SELECT 1 FROM dbo.auth_roles WHERE id = @AdminRoleId)
BEGIN
    INSERT INTO dbo.auth_roles (id, name, description, created_at) VALUES
    (@AdminRoleId, 'Admin', N'Quản trị viên toàn quyền hệ thống', GETDATE()),
    (@StaffRoleId, 'Staff', N'Nhân viên bán hàng & thu ngân tại quầy', GETDATE()),
    (@CustomerRoleId, 'Customer', N'Khách hàng thành viên đặt đồ uống', GETDATE());
END

-- Permissions
DECLARE @PermMenu UNIQUEIDENTIFIER = 'aaaaaaaa-1111-1111-1111-111111111111';
DECLARE @PermOrder UNIQUEIDENTIFIER = 'aaaaaaaa-2222-2222-2222-222222222222';
DECLARE @PermUser UNIQUEIDENTIFIER = 'aaaaaaaa-3333-3333-3333-333333333333';
DECLARE @PermPromo UNIQUEIDENTIFIER = 'aaaaaaaa-4444-4444-4444-444444444444';

IF NOT EXISTS (SELECT 1 FROM dbo.auth_permissions WHERE id = @PermMenu)
BEGIN
    INSERT INTO dbo.auth_permissions (id, code, name, description) VALUES
    (@PermMenu, 'MENU_MANAGE', N'Quản lý thực đơn', N'Thêm, sửa, xóa món ăn và danh mục'),
    (@PermOrder, 'ORDER_PROCESS', N'Xử lý đơn hàng', N'Xem và cập nhật trạng thái đơn hàng'),
    (@PermUser, 'USER_MANAGE', N'Quản lý người dùng', N'Quản lý danh sách tài khoản và phân quyền'),
    (@PermPromo, 'PROMO_MANAGE', N'Quản lý khuyến mãi', N'Tạo và quản lý mã giảm giá voucher');

    INSERT INTO dbo.auth_role_permissions (role_id, permission_id) VALUES
    (@AdminRoleId, @PermMenu),
    (@AdminRoleId, @PermOrder),
    (@AdminRoleId, @PermUser),
    (@AdminRoleId, @PermPromo),
    (@StaffRoleId, @PermOrder);
END

-- Users (Default Password: Admin@123)
DECLARE @AdminUserId UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
DECLARE @StaffUserId UNIQUEIDENTIFIER = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
DECLARE @CustomerUserId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

IF NOT EXISTS (SELECT 1 FROM dbo.auth_users WHERE id = @AdminUserId)
BEGIN
    INSERT INTO dbo.auth_users (id, role_id, email, password_hash, full_name, phone_number, avatar_url, address, is_active, created_at, updated_at)
    VALUES 
    (@AdminUserId, @AdminRoleId, 'admin@lowlandcoffee.com', '$2a$11$dbMvRxSU6tmWeR8q4A.P4O1exj54XyuhgY9wkycQSmb7VepqCgw0S', N'Quản Trị Viên', '0901122334', 'https://placehold.co/150', N'123 Nguyễn Huệ, Q.1, TP.HCM', 1, GETDATE(), GETDATE()),
    (@StaffUserId, @StaffRoleId, 'staff@lowlandcoffee.com', '$2a$11$dbMvRxSU6tmWeR8q4A.P4O1exj54XyuhgY9wkycQSmb7VepqCgw0S', N'Thu Ngân 01', '0902233445', 'https://placehold.co/150', N'LowLand Landmark 81, Bình Thạnh', 1, GETDATE(), GETDATE()),
    (@CustomerUserId, @CustomerRoleId, 'customer@gmail.com', '$2a$11$dbMvRxSU6tmWeR8q4A.P4O1exj54XyuhgY9wkycQSmb7VepqCgw0S', N'Nguyễn Văn Khách', '0903344556', 'https://placehold.co/150', N'456 Lê Lợi, Q.1, TP.HCM', 1, GETDATE(), GETDATE());
END
GO

-- =========================================================================
-- 2. DATABASE: Coffee_ProductDB (Product / Catalog Service)
-- =========================================================================
USE Coffee_ProductDB;
GO

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

-- =========================================================================
-- 3. DATABASE: Coffee_OrderDB (Order & Payment Service)
-- =========================================================================
USE Coffee_OrderDB;
GO

DECLARE @CustomerId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
DECLARE @OrderId1 UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @OrderId2 UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000002';
DECLARE @Prod1 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @Prod3 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';

IF NOT EXISTS (SELECT 1 FROM dbo.order_orders WHERE id = @OrderId1)
BEGIN
    INSERT INTO dbo.order_orders (id, user_id, order_code, total_amount, discount_amount, final_amount, status, note, created_at, updated_at)
    VALUES 
    (@OrderId1, @CustomerId, 'ORD-20261004-001', 45000.00, 10000.00, 35000.00, 'COMPLETED', N'Cho nhiều đá riêng', DATEADD(MINUTE, -60, GETDATE()), DATEADD(MINUTE, -50, GETDATE())),
    (@OrderId2, @CustomerId, 'ORD-20261004-002', 57000.00, 0.00, 57000.00, 'PROCESSING', N'Ít ngọt 50%', DATEADD(MINUTE, -10, GETDATE()), DATEADD(MINUTE, -10, GETDATE()));

    INSERT INTO dbo.order_items (order_id, product_id, product_name, unit_price, quantity, options_summary, subtotal)
    VALUES
    (@OrderId1, @Prod1, 'Phin Sua Da LowLand', 35000.00, 1, 'Size M (+6k), Thach Ca Phe (+10k)', 45000.00),
    (@OrderId2, @Prod3, 'Tra Sen Vang Hat Sen', 51000.00, 1, 'Size L (+12k), Hat Sen Them (+10k)', 57000.00);

    INSERT INTO dbo.order_payments (order_id, payment_method, transaction_code, amount, payment_status, paid_at, created_at)
    VALUES
    (@OrderId1, 'VNPAY', 'VNPAY_TXN_99887766', 35000.00, 'SUCCESS', DATEADD(MINUTE, -55, GETDATE()), DATEADD(MINUTE, -60, GETDATE())),
    (@OrderId2, 'CASH', NULL, 57000.00, 'PENDING', NULL, DATEADD(MINUTE, -10, GETDATE()));
END
GO

-- =========================================================================
-- 4. DATABASE: Coffee_EngagementDB (Engagement / Promotion Service)
-- =========================================================================
USE Coffee_EngagementDB;
GO

-- UUID cố định cho đồng bộ liên Microservices
DECLARE @Voucher1 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000001';
DECLARE @Voucher2 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000002';
DECLARE @Voucher3 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000003';
DECLARE @CustomerIdEngage UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc'; -- auth_users (Customer)
DECLARE @OrderId1Engage UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';   -- order_orders

-- Tiers UUIDs
DECLARE @TierBronze UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000001';
DECLARE @TierSilver UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000002';
DECLARE @TierGold UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000003';
DECLARE @TierDiamond UNIQUEIDENTIFIER = '41000000-0000-0000-0000-000000000004';

-- Member & Point Transaction UUIDs
DECLARE @Member1 UNIQUEIDENTIFIER = '42000000-0000-0000-0000-000000000001';
DECLARE @PointTx1 UNIQUEIDENTIFIER = '43000000-0000-0000-0000-000000000001';
DECLARE @PointTx2 UNIQUEIDENTIFIER = '43000000-0000-0000-0000-000000000002';

-- 1. Nạp Danh mục 4 Hạng thành viên (Loyalty Tiers)
IF NOT EXISTS (SELECT 1 FROM dbo.loyalty_tiers WHERE id = @TierBronze)
BEGIN
    INSERT INTO dbo.loyalty_tiers (id, tier_code, tier_name, min_points, point_multiplier, birthday_discount_percent, free_topping_min_order, free_topping_count, benefits_description, created_at)
    VALUES
    (@TierBronze, 'BRONZE', N'Hạng Đồng (Member)', 0, 1.00, 5.00, 0.00, 0, N'Tích điểm chuẩn 10.000đ = 1đ, tặng voucher giảm 5% tháng sinh nhật', GETDATE()),
    (@TierSilver, 'SILVER', N'Hạng Bạc (Silver)', 100, 1.10, 10.00, 100000.00, 1, N'Tích điểm x1.1, giảm 10% tháng sinh nhật, miễn phí 1 topping cho đơn > 100k', GETDATE()),
    (@TierGold, 'GOLD', N'Hạng Vàng (Gold)', 300, 1.25, 15.00, 130000.00, 2, N'Tích điểm x1.25, giảm 15% tháng sinh nhật, miễn phí 2 topping cho đơn > 130k', GETDATE()),
    (@TierDiamond, 'DIAMOND', N'Hạng Kim Cương (Diamond)', 600, 1.50, 20.00, 150000.00, 3, N'Tích điểm x1.5, giảm 20% tháng sinh nhật, tặng voucher -100% món giá thấp nhất, miễn phí 3 topping cho đơn > 150k', GETDATE());
END

-- 2. Nạp Danh sách Voucher khuyến mãi
IF NOT EXISTS (SELECT 1 FROM dbo.promo_vouchers WHERE id = @Voucher1)
BEGIN
    INSERT INTO dbo.promo_vouchers (id, code, title, description, discount_type, discount_value, max_discount_amount, min_order_value, total_quantity, used_quantity, start_date, end_date, is_active, created_at, updated_at)
    VALUES 
    (@Voucher1, 'BANMOI', N'Ưu đãi chào bạn mới 20%', N'Giảm 20% tối đa 30.000 VNĐ cho khách hàng mới', 'PERCENTAGE', 20.00, 30000.00, 40000.00, 1000, 15, '2026-01-01', '2026-12-31', 1, GETDATE(), GETDATE()),
    (@Voucher2, 'GIAM10K', N'Giảm 10K đơn từ 40K', N'Giảm trực tiếp 10.000 VNĐ cho đơn từ 40.000 VNĐ', 'FIXED_AMOUNT', 10000.00, 10000.00, 40000.00, 500, 8, '2026-01-01', '2026-12-31', 1, GETDATE(), GETDATE()),
    (@Voucher3, 'FREESHIP', N'Mã ưu đãi phí giao hàng 15K', N'Giảm 15.000 VNĐ phí giao hàng cho đơn từ 60.000 VNĐ', 'FIXED_AMOUNT', 15000.00, 15000.00, 60000.00, 300, 0, '2026-01-01', '2026-12-31', 1, GETDATE(), GETDATE());

    -- Lịch sử dùng voucher
    INSERT INTO dbo.promo_voucher_usages (voucher_id, user_id, order_id, discount_amount, used_at)
    VALUES (@Voucher2, @CustomerIdEngage, @OrderId1Engage, 10000.00, DATEADD(MINUTE, -60, GETDATE()));
END

-- 3. Nạp Hồ sơ thành viên & Giao dịch tích điểm mẫu
IF NOT EXISTS (SELECT 1 FROM dbo.loyalty_members WHERE id = @Member1)
BEGIN
    INSERT INTO dbo.loyalty_members (id, user_id, tier_id, current_points, total_accumulated_points, created_at, updated_at)
    VALUES (@Member1, @CustomerIdEngage, @TierSilver, 120, 150, DATEADD(MONTH, -1, GETDATE()), GETDATE());

    INSERT INTO dbo.loyalty_point_transactions (id, member_id, order_id, transaction_type, points_changed, balance_after, reason, created_at)
    VALUES
    (@PointTx1, @Member1, @OrderId1Engage, 'EARN', 150, 150, N'Tích điểm khi hoàn tất đơn hàng ORD-20261004-001', DATEADD(MINUTE, -60, GETDATE())),
    (@PointTx2, @Member1, NULL, 'REDEEM', -30, 120, N'Quy đổi 30 điểm giảm 30.000 VNĐ', DATEADD(MINUTE, -30, GETDATE()));
END
GO

-- =========================================================================
-- 5. DATABASE: Coffee_NotificationDB (Notification Service)
-- =========================================================================
USE Coffee_NotificationDB;
GO

DECLARE @CustomerIdNotif UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
DECLARE @Notif1 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
DECLARE @Notif2 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000002';
DECLARE @OrderId1Notif UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.notif_notifications WHERE id = @Notif1)
BEGIN
    INSERT INTO dbo.notif_notifications (id, user_id, title, content, type, reference_id, is_read, created_at)
    VALUES 
    (@Notif1, @CustomerIdNotif, 'Don hang hoan tat!', N'Đơn hàng ORD-20261004-001 của bạn đã hoàn thành. Cảm ơn bạn đã lựa chọn LowLand Coffee!', 'ORDER_STATUS', @OrderId1Notif, 1, DATEADD(MINUTE, -50, GETDATE())),
    (@Notif2, @CustomerIdNotif, 'Uu dai chao ban moi', N'Nhập mã BANMOI để nhận giảm 20% cho đơn hàng tiếp theo của bạn.', 'PROMOTION', NULL, 0, DATEADD(DAY, -1, GETDATE()));

    INSERT INTO dbo.notif_notification_logs (notification_id, channel, status, sent_at)
    VALUES 
    (@Notif1, 'PUSH_FCM', 'SENT', DATEADD(MINUTE, -50, GETDATE())),
    (@Notif2, 'EMAIL', 'SENT', DATEADD(DAY, -1, GETDATE()));
END
GO
