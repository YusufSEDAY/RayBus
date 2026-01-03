-- NOT: Bu script, view ve stored procedure'leri yeniden oluşturarak
--      yeni schema referanslarını kullanır.

USE [RayBusDB]
GO



-- ÖRNEK: vw_Admin_Dashboard_Istatistikleri GÜNCELLEMESİ

PRINT 'Örnek: vw_Admin_Dashboard_Istatistikleri güncelleniyor...';

IF OBJECT_ID('report.vw_Admin_Dashboard_Istatistikleri', 'V') IS NOT NULL
    DROP VIEW report.vw_Admin_Dashboard_Istatistikleri;
GO

CREATE VIEW report.vw_Admin_Dashboard_Istatistikleri
AS
SELECT 
    -- Kullanıcı İstatistikleri (app schema)
    (SELECT COUNT(*) FROM app.Users) AS TotalUsers,
    (SELECT COUNT(*) FROM app.Users WHERE Status = 1) AS ActiveUsers,
    
    -- Rezervasyon İstatistikleri (app schema)
    (SELECT COUNT(*) FROM app.Reservations) AS TotalReservations,
    (SELECT COUNT(*) FROM app.Reservations WHERE Status != 'Cancelled') AS ActiveReservations,
    
    -- Sefer İstatistikleri (app schema)
    (SELECT COUNT(*) FROM app.Trips) AS TotalTrips,
    (SELECT COUNT(*) FROM app.Trips WHERE Status = 1) AS ActiveTrips,
    
    -- Gelir İstatistikleri (app schema)
    (SELECT ISNULL(SUM(Amount), 0) FROM app.Payments WHERE Status = 'Completed') AS TotalRevenue,
    
    -- Son Güncelleme Tarihi
    GETDATE() AS SonGuncellemeTarihi;
GO

PRINT '   ✅ vw_Admin_Dashboard_Istatistikleri güncellendi';
PRINT '';


-- ÖRNEK: sp_Kullanici_Kayit GÜNCELLEMESİ

PRINT 'Örnek: sp_Kullanici_Kayit güncelleniyor...';

IF OBJECT_ID('[proc].sp_Kullanici_Kayit', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Kullanici_Kayit;
GO


CREATE PROCEDURE [proc].sp_Kullanici_Kayit
    @FullName NVARCHAR(150),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(300),
    @Phone NVARCHAR(30) = NULL,
    @RoleName NVARCHAR(50) = 'Müşteri'
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Email kontrolü (app schema)
    IF EXISTS (SELECT 1 FROM app.Users WHERE Email = @Email)
    BEGIN
        RAISERROR('Bu email adresi zaten kullanılıyor.', 16, 1);
        RETURN;
    END
    
    -- Role bulma (app schema)
    DECLARE @RoleID INT;
    SELECT @RoleID = RoleID FROM app.Roles WHERE RoleName = @RoleName;
    
    IF @RoleID IS NULL
    BEGIN
        RAISERROR('Geçersiz rol adı.', 16, 1);
        RETURN;
    END
    
    -- Kullanıcı oluşturma (app schema)
    INSERT INTO app.Users (RoleID, FullName, Email, PasswordHash, Phone)
    VALUES (@RoleID, @FullName, @Email, @PasswordHash, @Phone);
    
    SELECT SCOPE_IDENTITY() AS UserID;
END;
GO

PRINT '   ✅ sp_Kullanici_Kayit güncellendi';
PRINT '';



