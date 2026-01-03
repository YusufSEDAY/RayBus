-- Admin kullanıcısı oluşturma scripti
-- Not: Şifreler BCrypt ile hash'lenmiş olmalıdır
-- Bu script sadece örnek amaçlıdır, gerçek şifre hash'i backend'den alınmalıdır

-- Önce Admin rolünün var olduğundan emin ol
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = 'Admin')
BEGIN
    INSERT INTO dbo.Roles (RoleName)
    VALUES ('Admin');
END
GO

-- Admin kullanıcısı oluştur (şifre: Admin123!)
-- Not: Bu şifre hash'i örnek amaçlıdır, gerçek hash backend'den alınmalıdır
-- BCrypt hash örneği: $2a$11$KIXxKIXxKIXxKIXxKIXxOu (gerçek hash değil, örnek)

DECLARE @AdminRoleID INT = (SELECT RoleID FROM dbo.Roles WHERE RoleName = 'Admin');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = 'admin@raybus.com')
BEGIN
    INSERT INTO dbo.Users (RoleID, FullName, Email, PasswordHash, Phone, Status, CreatedAt)
    VALUES (
        @AdminRoleID,
        'Admin User',
        'admin@raybus.com',
        '$2a$11$KIXxKIXxKIXxKIXxKIXxOu', -- Bu örnek bir hash, gerçek hash backend'den alınmalı
        '05551234567',
        1,
        GETUTCDATE()
    );
    
    PRINT 'Admin kullanıcısı oluşturuldu: admin@raybus.com';
END
ELSE
BEGIN
    PRINT 'Admin kullanıcısı zaten mevcut.';
END
GO

