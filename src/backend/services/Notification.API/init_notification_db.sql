-- =========================================================================
-- SCRIPT KHỞI TẠO CƠ SỞ DỮ LIỆU: Coffee_NotificationDB (Notification Service)
-- Thiết kế theo chuẩn DBML dbdiagram.io (UUID / UNIQUEIDENTIFIER)
-- =========================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Coffee_NotificationDB')
BEGIN
    CREATE DATABASE Coffee_NotificationDB;
END
GO

USE Coffee_NotificationDB;
GO

-- 1. BẢNG notif_notifications
IF OBJECT_ID('dbo.notif_notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.notif_notifications (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        user_id UNIQUEIDENTIFIER NOT NULL,        -- Logical Ref -> auth_users.id
        title VARCHAR(200) NOT NULL,
        content NVARCHAR(MAX) NOT NULL,
        type VARCHAR(50) NOT NULL,                -- ORDER_STATUS, PROMOTION, SYSTEM
        reference_id UNIQUEIDENTIFIER NULL,       -- Optional: Order ID, Voucher ID
        is_read BIT NOT NULL DEFAULT 0,
        created_at DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END
GO

-- 2. BẢNG notif_notification_logs
IF OBJECT_ID('dbo.notif_notification_logs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.notif_notification_logs (
        id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        notification_id UNIQUEIDENTIFIER NULL,    -- Foreign key to notif_notifications.id
        channel VARCHAR(30) NOT NULL,             -- PUSH_FCM, EMAIL, SMS
        status VARCHAR(20) NOT NULL,              -- SENT, FAILED
        error_message NVARCHAR(MAX) NULL,
        sent_at DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_notif_notification_logs_notifications FOREIGN KEY (notification_id) REFERENCES dbo.notif_notifications(id) ON DELETE CASCADE
    );
END
GO
