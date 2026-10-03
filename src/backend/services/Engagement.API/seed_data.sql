-- =========================================================================
-- SEED DATA: Coffee_EngagementDB (Engagement / Promotion Service)
-- =========================================================================

USE Coffee_EngagementDB;
GO

-- 1. UUID Cố định để đồng bộ giữa các Microservices
DECLARE @Voucher1 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000001';
DECLARE @Voucher2 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000002';
DECLARE @CustomerId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc'; -- auth_users (Customer)
DECLARE @OrderId1 UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';   -- order_orders

IF NOT EXISTS (SELECT 1 FROM dbo.promo_vouchers WHERE id = @Voucher1)
BEGIN
    -- 2. Nạp Danh sách Voucher khuyến mãi
    INSERT INTO dbo.promo_vouchers (id, code, description, discount_type, discount_value, max_discount_amount, min_order_value, total_quantity, used_quantity, start_date, end_date, is_active, created_at)
    VALUES 
    (@Voucher1, 'BANMOI', N'Giảm 20% tối đa 30.000 VNĐ cho khách hàng mới', 'PERCENTAGE', 20.00, 30000.00, 40000.00, 1000, 15, '2026-01-01', '2026-12-31', 1, GETDATE()),
    (@Voucher2, 'GIAM10K', N'Giảm trực tiếp 10.000 VNĐ cho đơn từ 40.000 VNĐ', 'FIXED_AMOUNT', 10000.00, 10000.00, 40000.00, 500, 8, '2026-01-01', '2026-12-31', 1, GETDATE());

    -- 3. Nạp Lịch sử sử dụng Voucher (Voucher Usages)
    INSERT INTO dbo.promo_voucher_usages (voucher_id, user_id, order_id, used_at)
    VALUES (@Voucher2, @CustomerId, @OrderId1, DATEADD(MINUTE, -60, GETDATE()));
END
GO
