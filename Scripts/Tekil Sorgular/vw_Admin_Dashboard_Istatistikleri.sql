-- =============================================
-- View: vw_Admin_Dashboard_Istatistikleri
-- Açıklama: Admin paneli dashboard için tüm istatistikleri tek sorguda getirir
-- Performans: 7 ayrı sorgu yerine 1 sorgu kullanır
-- =============================================

-- Önce view'i drop et (varsa)
IF OBJECT_ID('dbo.vw_Admin_Dashboard_Istatistikleri', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Admin_Dashboard_Istatistikleri;
GO

CREATE VIEW dbo.vw_Admin_Dashboard_Istatistikleri
AS
SELECT 
    -- Kullanıcı İstatistikleri
    (SELECT COUNT(*) FROM dbo.Users) AS TotalUsers,
    (SELECT COUNT(*) FROM dbo.Users WHERE Status = 1) AS ActiveUsers,
    
    -- Rezervasyon İstatistikleri
    (SELECT COUNT(*) FROM dbo.Reservations) AS TotalReservations,
    (SELECT COUNT(*) FROM dbo.Reservations WHERE Status != 'Cancelled') AS ActiveReservations,
    
    -- Sefer İstatistikleri
    (SELECT COUNT(*) FROM dbo.Trips) AS TotalTrips,
    (SELECT COUNT(*) FROM dbo.Trips WHERE Status = 1) AS ActiveTrips,
    
    -- Gelir İstatistikleri
    (SELECT ISNULL(SUM(Amount), 0) FROM dbo.Payments WHERE Status = 'Completed') AS TotalRevenue,
    
    -- Son Güncelleme Tarihi
    GETDATE() AS SonGuncellemeTarihi;
GO

-- View'i test et
-- SELECT * FROM dbo.vw_Admin_Dashboard_Istatistikleri;
GO

