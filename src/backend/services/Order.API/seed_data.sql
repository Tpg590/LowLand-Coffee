-- =========================================================================
-- SEED DATA: Coffee_OrderDB (Order & Payment Service)
-- =========================================================================

USE Coffee_OrderDB;
GO

-- 1. UUID Cố định để đồng bộ giữa các Microservices
DECLARE @CustomerId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc'; -- auth_users (Customer)
DECLARE @OrderId1 UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @OrderId2 UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000002';
DECLARE @Prod1 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001'; -- Phin Sữa Đá
DECLARE @Prod3 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004'; -- Trà Sen Vàng

IF NOT EXISTS (SELECT 1 FROM dbo.order_orders WHERE id = @OrderId1)
BEGIN
    -- 2. Nạp Đơn hàng mẫu
    INSERT INTO dbo.order_orders (id, user_id, order_code, total_amount, discount_amount, final_amount, status, note, created_at, updated_at)
    VALUES 
    (@OrderId1, @CustomerId, 'ORD-20261004-001', 45000.00, 10000.00, 35000.00, 'COMPLETED', N'Cho nhiều đá riêng', DATEADD(MINUTE, -60, GETDATE()), DATEADD(MINUTE, -50, GETDATE())),
    (@OrderId2, @CustomerId, 'ORD-20261004-002', 57000.00, 0.00, 57000.00, 'PROCESSING', N'Ít ngọt 50%', DATEADD(MINUTE, -10, GETDATE()), DATEADD(MINUTE, -10, GETDATE()));

    -- 3. Nạp Chi tiết món trong đơn hàng (Order Items)
    INSERT INTO dbo.order_items (order_id, product_id, product_name, unit_price, quantity, options_summary, subtotal)
    VALUES
    (@OrderId1, @Prod1, 'Phin Sua Da LowLand', 35000.00, 1, 'Size M (+6k), Thach Ca Phe (+10k)', 45000.00),
    (@OrderId2, @Prod3, 'Tra Sen Vang Hat Sen', 51000.00, 1, 'Size L (+12k), Hat Sen Them (+10k)', 57000.00);

    -- 4. Nạp Thông tin thanh toán (Order Payments)
    INSERT INTO dbo.order_payments (order_id, payment_method, transaction_code, amount, payment_status, paid_at, created_at)
    VALUES
    (@OrderId1, 'VNPAY', 'VNPAY_TXN_99887766', 35000.00, 'SUCCESS', DATEADD(MINUTE, -55, GETDATE()), DATEADD(MINUTE, -60, GETDATE())),
    (@OrderId2, 'CASH', NULL, 57000.00, 'PENDING', NULL, DATEADD(MINUTE, -10, GETDATE()));
END
GO
