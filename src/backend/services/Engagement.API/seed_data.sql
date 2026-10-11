-- =========================================================================
-- SEED DATA: Coffee_EngagementDB (Engagement / Promotion Service)
-- =========================================================================

USE Coffee_EngagementDB;
GO

-- UUID cố định để đồng bộ liên Microservices
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
