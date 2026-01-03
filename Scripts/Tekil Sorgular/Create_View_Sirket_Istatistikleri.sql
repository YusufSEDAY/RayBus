-- =============================================
-- View: vw_Sirket_Istatistikleri
-- Açıklama: Şirket paneli için istatistikleri getirir
-- Şirket bazlı sefer, rezervasyon, gelir ve araç istatistiklerini içerir
-- Parametre: @SirketID (view kullanımında WHERE ile filtrelenir)
-- =============================================

-- Önce view'i drop et (varsa)
IF OBJECT_ID('dbo.vw_Sirket_Istatistikleri', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Sirket_Istatistikleri;
GO

CREATE VIEW dbo.vw_Sirket_Istatistikleri
AS
SELECT 
    -- Şirket Bilgileri
    U.UserID AS SirketID,
    U.FullName AS SirketAdi,
    U.Email AS SirketEmail,
    
    -- Sefer İstatistikleri
    (SELECT COUNT(*) 
     FROM dbo.Trips T
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID) AS ToplamSefer,
    
    (SELECT COUNT(*) 
     FROM dbo.Trips T
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID AND T.Status = 1) AS AktifSefer,
    
    (SELECT COUNT(*) 
     FROM dbo.Trips T
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID AND T.Status = 0) AS IptalSefer,
    
    -- Rezervasyon İstatistikleri
    (SELECT COUNT(*) 
     FROM dbo.Reservations R
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND R.Status != 'Cancelled') AS ToplamRezervasyon,
    
    (SELECT COUNT(*) 
     FROM dbo.Reservations R
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND (R.Status = 'Reserved' OR R.Status = 'Confirmed')) AS AktifRezervasyon,
    
    (SELECT COUNT(*) 
     FROM dbo.Reservations R
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND R.Status = 'Cancelled') AS IptalRezervasyon,
    
    -- Gelir İstatistikleri
    (SELECT ISNULL(SUM(P.Amount), 0)
     FROM dbo.Payments P
     INNER JOIN dbo.Reservations R ON P.ReservationID = R.ReservationID
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND P.Status = 'Completed') AS ToplamGelir,
    
    (SELECT ISNULL(SUM(P.Amount), 0)
     FROM dbo.Payments P
     INNER JOIN dbo.Reservations R ON P.ReservationID = R.ReservationID
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND P.Status = 'Completed'
       AND P.PaymentDate >= DATEADD(MONTH, -1, GETDATE())) AS SonBirAyGelir,
    
    -- Araç İstatistikleri
    (SELECT COUNT(*) 
     FROM dbo.Vehicles V
     WHERE V.CompanyID = U.UserID AND V.Active = 1) AS ToplamArac,
    
    (SELECT COUNT(*) 
     FROM dbo.Vehicles V
     WHERE V.CompanyID = U.UserID 
       AND V.Active = 1 
       AND V.VehicleType = 'Bus') AS OtobusSayisi,
    
    (SELECT COUNT(*) 
     FROM dbo.Vehicles V
     WHERE V.CompanyID = U.UserID 
       AND V.Active = 1 
       AND V.VehicleType = 'Train') AS TrenSayisi,
    
    -- Dolu Koltuk Oranı (Ortalama)
    (SELECT CASE 
        WHEN COUNT(DISTINCT T.TripID) > 0 THEN
            CAST(SUM(CASE WHEN TS.IsReserved = 1 THEN 1 ELSE 0 END) AS FLOAT) / 
            CAST(COUNT(*) AS FLOAT) * 100
        ELSE 0
     END
     FROM dbo.Trips T
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     INNER JOIN dbo.TripSeats TS ON T.TripID = TS.TripID
     WHERE V.CompanyID = U.UserID 
       AND T.Status = 1) AS OrtalamaDoluKoltukOrani,
    
    -- Bu Ay Eklenen Sefer Sayısı
    (SELECT COUNT(*) 
     FROM dbo.Trips T
     INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
     WHERE V.CompanyID = U.UserID 
       AND T.CreatedAt >= DATEADD(MONTH, -1, GETDATE())) AS BuAyEklenenSefer,
    
    -- Son Güncelleme Tarihi
    GETDATE() AS SonGuncellemeTarihi

FROM dbo.Users U
INNER JOIN dbo.Roles R ON U.RoleID = R.RoleID
WHERE R.RoleName = 'Şirket' AND U.Status = 1;
GO

-- View'i test et
-- SELECT * FROM dbo.vw_Sirket_Istatistikleri WHERE SirketID = 11;

-- View'in doğru çalıştığını test etmek için:
-- 1. Tüm şirketleri listele:
--    SELECT SirketID, SirketAdi FROM dbo.vw_Sirket_Istatistikleri;
-- 
-- 2. Belirli bir şirket için istatistikleri göster:
--    SELECT * FROM dbo.vw_Sirket_Istatistikleri WHERE SirketID = 11;
--
-- 3. View'deki sorguları manuel test et:
--    SELECT COUNT(*) FROM dbo.Trips T
--    INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
--    WHERE V.CompanyID = 11;
GO

