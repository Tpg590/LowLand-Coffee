-- =========================================================================
-- SEED DATA: Coffee_AuthDB (Auth & User Service)
-- =========================================================================

USE Coffee_AuthDB;
GO

-- 1. Định nghĩa các UUID cố định để đồng bộ giữa các Microservices
DECLARE @AdminRoleId UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @StaffRoleId UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222222';
DECLARE @CustomerRoleId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333333';

-- 2. Nạp Roles
IF NOT EXISTS (SELECT 1 FROM dbo.auth_roles WHERE id = @AdminRoleId)
BEGIN
    INSERT INTO dbo.auth_roles (id, name, description, created_at) VALUES
    (@AdminRoleId, 'Admin', N'Quản trị viên toàn quyền hệ thống', GETDATE()),
    (@StaffRoleId, 'Staff', N'Nhân viên bán hàng & thu ngân tại quầy', GETDATE()),
    (@CustomerRoleId, 'Customer', N'Khách hàng thành viên đặt đồ uống', GETDATE());
END

-- 3. Nạp Permissions
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

    -- Gán Permissions cho Role
    INSERT INTO dbo.auth_role_permissions (role_id, permission_id) VALUES
    (@AdminRoleId, @PermMenu),
    (@AdminRoleId, @PermOrder),
    (@AdminRoleId, @PermUser),
    (@AdminRoleId, @PermPromo),
    (@StaffRoleId, @PermOrder);
END

-- 4. Nạp Users (Password hash mặc định: Admin@123)
DECLARE @AdminUserId UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
DECLARE @StaffUserId UNIQUEIDENTIFIER = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
DECLARE @CustomerUserId UNIQUEIDENTIFIER = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

IF NOT EXISTS (SELECT 1 FROM dbo.auth_users WHERE id = @AdminUserId)
BEGIN
    INSERT INTO dbo.auth_users (id, role_id, email, password_hash, full_name, phone_number, avatar_url, address, is_active, created_at, updated_at)
    VALUES 
    (@AdminUserId, @AdminRoleId, 'admin@lowlandcoffee.com', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', N'Quản Trị Viên', '0901122334', 'https://placehold.co/150', N'123 Nguyễn Huệ, Q.1, TP.HCM', 1, GETDATE(), GETDATE()),
    (@StaffUserId, @StaffRoleId, 'staff@lowlandcoffee.com', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', N'Thu Ngân 01', '0902233445', 'https://placehold.co/150', N'LowLand Landmark 81, Bình Thạnh', 1, GETDATE(), GETDATE()),
    (@CustomerUserId, @CustomerRoleId, 'customer@gmail.com', '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', N'Nguyễn Văn Khách', '0903344556', 'https://placehold.co/150', N'456 Lê Lợi, Q.1, TP.HCM', 1, GETDATE(), GETDATE());
END
GO
