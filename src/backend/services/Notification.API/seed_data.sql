-- =========================================================================
-- SEED DATA: Coffee_NotificationDB (Notification Service)
-- =========================================================================

USE Coffee_NotificationDB;
GO

-- 1. UUID Cố định để đồng bộ giữa các Microservices
DECLARE @CustomerId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc'; -- auth_users (Customer)
DECLARE @Notif1 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
DECLARE @Notif2 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000002';
DECLARE @OrderId1 UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';   -- order_orders

IF NOT EXISTS (SELECT 1 FROM dbo.notif_notifications WHERE id = @Notif1)
BEGIN
    -- 2. Nạp Danh sách Thông báo (Notifications)
    INSERT INTO dbo.notif_notifications (id, user_id, title, content, type, reference_id, is_read, created_at)
    VALUES 
    (@Notif1, @CustomerId, 'Don hang hoan tat!', N'Đơn hàng ORD-20261004-001 của bạn đã hoàn thành. Cảm ơn bạn đã lựa chọn LowLand Coffee!', 'ORDER_STATUS', @OrderId1, 1, DATEADD(MINUTE, -50, GETDATE())),
    (@Notif2, @CustomerId, 'Uu dai chao ban moi', N'Nhập mã BANMOI để nhận giảm 20% cho đơn hàng tiếp theo của bạn.', 'PROMOTION', NULL, 0, DATEADD(DAY, -1, GETDATE()));

    -- 3. Nạp Nhật ký gửi Thông báo (Notification Logs)
    INSERT INTO dbo.notif_notification_logs (notification_id, channel, status, sent_at)
    VALUES 
    (@Notif1, 'PUSH_FCM', 'SENT', DATEADD(MINUTE, -50, GETDATE())),
    (@Notif2, 'EMAIL', 'SENT', DATEADD(DAY, -1, GETDATE()));
END
GO
