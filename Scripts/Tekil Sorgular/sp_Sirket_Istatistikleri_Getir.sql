-- =============================================
-- Stored Procedure: sp_Sirket_Istatistikleri_Getir
-- Açıklama: Şirket istatistiklerini hesaplar (View fallback mekanizması için)
-- Parametre: @SirketID
-- =============================================

-- Önce stored procedure'ü drop et (varsa)
IF OBJECT_ID('dbo.sp_Sirket_Istatistikleri_Getir', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Sirket_Istatistikleri_Getir;
GO

CREATE PROCEDURE dbo.sp_Sirket_Istatistikleri_Getir
    @SirketID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Şirket bilgileri
    DECLARE @SirketAdi NVARCHAR(255) = NULL;
    DECLARE @SirketEmail NVARCHAR(255) = NULL;
    
    SELECT 
        @SirketAdi = FullName,
        @SirketEmail = Email
    FROM dbo.Users
    WHERE UserID = @SirketID;
    
    -- Şirkete ait araç ID'leri
    DECLARE @CompanyVehicles TABLE (VehicleID INT);
    INSERT INTO @CompanyVehicles
    SELECT VehicleID
    FROM dbo.Vehicles
    WHERE CompanyID = @SirketID;
    
    -- Şirkete ait sefer ID'leri
    DECLARE @CompanyTripIds TABLE (TripID INT);
    INSERT INTO @CompanyTripIds
    SELECT T.TripID
    FROM dbo.Trips T
    INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID;
    
    -- İstatistikleri hesapla
    SELECT 
        @SirketID AS SirketID,
        @SirketAdi AS SirketAdi,
        @SirketEmail AS SirketEmail,
        
        -- Sefer İstatistikleri
        (SELECT COUNT(*) FROM dbo.Trips T INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID) AS TotalTrips,
        (SELECT COUNT(*) FROM dbo.Trips T INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID WHERE T.Status = 1) AS ActiveTrips,
        (SELECT COUNT(*) FROM dbo.Trips T INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID WHERE T.Status = 0) AS IptalSefer,
        
        -- Rezervasyon İstatistikleri
        (SELECT COUNT(*) FROM dbo.Reservations R INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID WHERE R.Status != 'Cancelled') AS TotalReservations,
        (SELECT COUNT(*) FROM dbo.Reservations R INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID WHERE R.Status IN ('Reserved', 'Confirmed')) AS ActiveReservations,
        (SELECT COUNT(*) FROM dbo.Reservations R INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID WHERE R.Status = 'Cancelled') AS IptalRezervasyon,
        
        -- Gelir İstatistikleri
        (SELECT ISNULL(SUM(P.Amount), 0)
         FROM dbo.Payments P
         INNER JOIN dbo.Reservations R ON P.ReservationID = R.ReservationID
         INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID
         WHERE P.Status = 'Completed') AS ToplamGelir,
        
        (SELECT ISNULL(SUM(P.Amount), 0)
         FROM dbo.Payments P
         INNER JOIN dbo.Reservations R ON P.ReservationID = R.ReservationID
         INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID
         WHERE P.Status = 'Completed'
           AND P.PaymentDate >= DATEADD(MONTH, -1, GETDATE())) AS SonBirAyGelir,
        
        -- Araç İstatistikleri
        (SELECT COUNT(*) FROM dbo.Vehicles WHERE CompanyID = @SirketID AND Active = 1) AS ToplamArac,
        (SELECT COUNT(*) FROM dbo.Vehicles WHERE CompanyID = @SirketID AND Active = 1 AND VehicleType = 'Bus') AS OtobusSayisi,
        (SELECT COUNT(*) FROM dbo.Vehicles WHERE CompanyID = @SirketID AND Active = 1 AND VehicleType = 'Train') AS TrenSayisi,
        
        -- Dolu Koltuk Oranı
        (SELECT CASE 
            WHEN COUNT(*) > 0 THEN
                CAST(SUM(CASE WHEN TS.IsReserved = 1 THEN 1 ELSE 0 END) AS FLOAT) / 
                CAST(COUNT(*) AS FLOAT) * 100
            ELSE 0
         END
         FROM dbo.TripSeats TS
         INNER JOIN @CompanyTripIds CT ON TS.TripID = CT.TripID
         INNER JOIN dbo.Trips T ON TS.TripID = T.TripID
         WHERE T.Status = 1) AS OrtalamaDoluKoltukOrani,
        
        -- Bu Ay Eklenen Sefer
        (SELECT COUNT(*) 
         FROM dbo.Trips T
         INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID
         WHERE T.CreatedAt >= DATEADD(MONTH, -1, GETDATE())) AS BuAyEklenenSefer,
        
        -- Son Güncelleme Tarihi
        GETDATE() AS SonGuncellemeTarihi;
END;
GO

-- Stored procedure'ü test et
-- EXEC dbo.sp_Sirket_Istatistikleri_Getir @SirketID = 11;
GO

