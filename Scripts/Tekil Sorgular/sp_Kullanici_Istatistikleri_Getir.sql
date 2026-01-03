-- =============================================
-- Stored Procedure: sp_Kullanici_Istatistikleri_Getir
-- Açıklama: Kullanıcı istatistiklerini hesaplar (View fallback mekanizması için)
-- Parametre: @UserID
-- =============================================

-- Önce stored procedure'ü drop et (varsa)
IF OBJECT_ID('dbo.sp_Kullanici_Istatistikleri_Getir', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Kullanici_Istatistikleri_Getir;
GO

CREATE PROCEDURE dbo.sp_Kullanici_Istatistikleri_Getir
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- PaymentStatus = 'Paid' olan rezervasyonları al (iptal edilmemiş)
    DECLARE @PaidReservations TABLE (
        ReservationID INT,
        TripID INT,
        PaymentStatus NVARCHAR(50)
    );
    
    INSERT INTO @PaidReservations
    SELECT ReservationID, TripID, PaymentStatus
    FROM dbo.Reservations
    WHERE UserID = @UserID 
      AND Status != 'Cancelled' 
      AND PaymentStatus = 'Paid';
    
    -- Trip ID'leri
    DECLARE @TripIds TABLE (TripID INT);
    INSERT INTO @TripIds
    SELECT DISTINCT TripID
    FROM @PaidReservations;
    
    -- Reservation ID'leri
    DECLARE @ReservationIds TABLE (ReservationID INT);
    INSERT INTO @ReservationIds
    SELECT ReservationID
    FROM @PaidReservations;
    
    -- Payments tablosundan toplam harcama
    DECLARE @TotalFromPayments DECIMAL(18, 2) = 0;
    SELECT @TotalFromPayments = ISNULL(SUM(Amount), 0)
    FROM dbo.Payments
    WHERE ReservationID IN (SELECT ReservationID FROM @ReservationIds)
      AND Status = 'Paid';
    
    -- Eğer Payments'da kayıt yoksa, Trip fiyatlarını kullan
    DECLARE @TotalFromTrips DECIMAL(18, 2) = 0;
    IF @TotalFromPayments = 0
    BEGIN
        SELECT @TotalFromTrips = ISNULL(SUM(T.Price), 0)
        FROM dbo.Trips T
        INNER JOIN @PaidReservations PR ON T.TripID = PR.TripID;
    END
    
    -- Toplam harcama
    DECLARE @ToplamHarcama DECIMAL(18, 2) = CASE 
        WHEN @TotalFromPayments > 0 THEN @TotalFromPayments
        ELSE @TotalFromTrips
    END;
    
    -- Ortalama fiyat
    DECLARE @OrtalamaFiyat DECIMAL(18, 2) = 0;
    IF EXISTS (SELECT 1 FROM dbo.Payments WHERE ReservationID IN (SELECT ReservationID FROM @ReservationIds) AND Status = 'Paid')
    BEGIN
        SELECT @OrtalamaFiyat = ISNULL(AVG(Amount), 0)
        FROM dbo.Payments
        WHERE ReservationID IN (SELECT ReservationID FROM @ReservationIds)
          AND Status = 'Paid';
    END
    ELSE IF EXISTS (SELECT 1 FROM @TripIds)
    BEGIN
        SELECT @OrtalamaFiyat = ISNULL(AVG(Price), 0)
        FROM dbo.Trips
        WHERE TripID IN (SELECT TripID FROM @TripIds);
    END
    
    -- Seyahat sayıları
    DECLARE @ToplamSeyahat INT = 0;
    DECLARE @GelecekSeyahat INT = 0;
    DECLARE @GecmisSeyahat INT = 0;
    
    SELECT 
        @ToplamSeyahat = COUNT(*),
        @GelecekSeyahat = SUM(CASE WHEN DepartureDate >= CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END),
        @GecmisSeyahat = SUM(CASE WHEN DepartureDate < CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END)
    FROM dbo.Trips
    WHERE TripID IN (SELECT TripID FROM @TripIds);
    
    -- Sonuç döndür
    SELECT 
        @UserID AS UserID,
        @ToplamHarcama AS ToplamHarcama,
        @OrtalamaFiyat AS OrtalamaSeyahatFiyati,
        @ToplamSeyahat AS ToplamSeyahatSayisi,
        @GelecekSeyahat AS GelecekSeyahatSayisi,
        @GecmisSeyahat AS GecmisSeyahatSayisi,
        (SELECT COUNT(*) FROM dbo.Reservations WHERE UserID = @UserID AND Status != 'Cancelled') AS ToplamRezervasyonSayisi,
        GETDATE() AS SonGuncellemeTarihi;
END;
GO

-- Stored procedure'ü test et
-- EXEC dbo.sp_Kullanici_Istatistikleri_Getir @UserID = 1;
GO

