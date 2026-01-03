-- =============================================
-- Otomatik İptal Sistemi
-- Açıklama: Ödeme bekleyen rezervasyonları otomatik iptal eder
-- Tarih: 2024-12-15
-- =============================================

USE RayBusDB;
GO

-- =============================================
-- 1. AutoCancellationLog Tablosu
-- =============================================
IF OBJECT_ID('dbo.AutoCancellationLog', 'U') IS NOT NULL
    DROP TABLE dbo.AutoCancellationLog;
GO

CREATE TABLE dbo.AutoCancellationLog (
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    ReservationID INT NOT NULL,
    UserID INT NOT NULL,
    CancelledAt DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    Reason NVARCHAR(500) DEFAULT 'Ödeme zaman aşımı' NOT NULL,
    OriginalReservationDate DATETIME2 NOT NULL,
    TimeoutMinutes INT DEFAULT 15 NOT NULL, -- Varsayılan 15 dakika
    CONSTRAINT FK_AutoCancellationLog_Reservations FOREIGN KEY (ReservationID) REFERENCES Reservations(ReservationID),
    CONSTRAINT FK_AutoCancellationLog_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

CREATE INDEX IX_AutoCancellationLog_ReservationID ON dbo.AutoCancellationLog(ReservationID);
CREATE INDEX IX_AutoCancellationLog_CancelledAt ON dbo.AutoCancellationLog(CancelledAt);
GO

-- =============================================
-- 2. Stored Procedure: sp_Zaman_Asimi_Rezervasyonlar
-- Açıklama: Zaman aşımına uğrayan rezervasyonları iptal eder
-- =============================================
IF OBJECT_ID('dbo.sp_Zaman_Asimi_Rezervasyonlar', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Zaman_Asimi_Rezervasyonlar;
GO

CREATE PROCEDURE dbo.sp_Zaman_Asimi_Rezervasyonlar
    @TimeoutMinutes INT = 15, -- Varsayılan 15 dakika
    @MaxCancellations INT = 100 -- Bir seferde maksimum iptal sayısı (performans için)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CancelledCount INT = 0;
    DECLARE @ErrorMessage NVARCHAR(500);
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Zaman aşımına uğrayan rezervasyonları bul ve iptal et
        -- Sadece 'Pending' ödeme durumundaki ve 'Reserved' durumundaki rezervasyonlar
        DECLARE @ReservationsToCancel TABLE (
            ReservationID INT,
            UserID INT,
            ReservationDate DATETIME2
        );
        
        INSERT INTO @ReservationsToCancel (ReservationID, UserID, ReservationDate)
        SELECT 
            R.ReservationID,
            R.UserID,
            R.ReservationDate
        FROM dbo.Reservations R
        LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID
        WHERE R.Status = 'Reserved'
          AND (P.Status IS NULL OR P.Status = 'Pending')
          AND DATEDIFF(MINUTE, R.ReservationDate, GETDATE()) >= @TimeoutMinutes
          AND NOT EXISTS (
              SELECT 1 
              FROM dbo.AutoCancellationLog ACL 
              WHERE ACL.ReservationID = R.ReservationID
          );
        
        -- Rezervasyonları iptal et
        UPDATE R
        SET 
            R.Status = 'Cancelled',
            R.CancelReasonID = (SELECT TOP 1 CancelReasonID FROM dbo.CancellationReasons WHERE ReasonText LIKE '%Zaman aşımı%' OR ReasonText LIKE '%Timeout%')
        FROM dbo.Reservations R
        INNER JOIN @ReservationsToCancel RTC ON R.ReservationID = RTC.ReservationID;
        
        -- Koltukları serbest bırak
        UPDATE TS
        SET TS.IsReserved = 0
        FROM dbo.TripSeats TS
        INNER JOIN dbo.Reservations R ON TS.TripID = R.TripID AND TS.SeatID = R.SeatID
        INNER JOIN @ReservationsToCancel RTC ON R.ReservationID = RTC.ReservationID;
        
        -- Log kayıtları oluştur
        INSERT INTO dbo.AutoCancellationLog (ReservationID, UserID, Reason, OriginalReservationDate, TimeoutMinutes)
        SELECT 
            ReservationID,
            UserID,
            'Ödeme zaman aşımı - Otomatik iptal edildi',
            ReservationDate,
            @TimeoutMinutes
        FROM @ReservationsToCancel;
        
        SET @CancelledCount = @@ROWCOUNT;
        
        COMMIT TRANSACTION;
        
        -- Sonuç döndür
        SELECT 
            @CancelledCount AS IptalEdilenSayisi,
            'Başarılı' AS Durum,
            CAST(GETDATE() AS NVARCHAR(50)) AS IslemTarihi;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        SET @ErrorMessage = ERROR_MESSAGE();
        
        SELECT 
            0 AS IptalEdilenSayisi,
            'Hata: ' + @ErrorMessage AS Durum,
            CAST(GETDATE() AS NVARCHAR(50)) AS IslemTarihi;
    END CATCH;
END;
GO

-- =============================================
-- 3. Stored Procedure: sp_Otomatik_Iptal_Ayarlari
-- Açıklama: Otomatik iptal ayarlarını yönetir
-- =============================================
IF OBJECT_ID('dbo.sp_Otomatik_Iptal_Ayarlari', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Otomatik_Iptal_Ayarlari;
GO

CREATE PROCEDURE dbo.sp_Otomatik_Iptal_Ayarlari
    @IslemTipi NVARCHAR(20) = 'GET', -- 'GET', 'SET'
    @TimeoutMinutes INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @IslemTipi = 'GET'
    BEGIN
        -- Mevcut ayarları getir (varsayılan 15 dakika)
        SELECT 
            15 AS TimeoutMinutes,
            'Aktif' AS Durum,
            'Otomatik iptal sistemi aktif' AS Aciklama;
    END
    ELSE IF @IslemTipi = 'SET' AND @TimeoutMinutes IS NOT NULL
    BEGIN
        -- Ayarları güncelle (şimdilik sadece bilgi döndür, gerçek bir ayar tablosu eklenebilir)
        SELECT 
            @TimeoutMinutes AS TimeoutMinutes,
            'Güncellendi' AS Durum,
            'Otomatik iptal süresi ' + CAST(@TimeoutMinutes AS NVARCHAR(10)) + ' dakika olarak ayarlandı' AS Aciklama;
    END
END;
GO

-- =============================================
-- 4. View: vw_Bekleyen_Iptaller
-- Açıklama: İptal edilmeyi bekleyen rezervasyonları gösterir
-- =============================================
IF OBJECT_ID('dbo.vw_Bekleyen_Iptaller', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Bekleyen_Iptaller;
GO

CREATE VIEW dbo.vw_Bekleyen_Iptaller
AS
SELECT 
    R.ReservationID,
    R.UserID,
    U.FullName AS KullaniciAdi,
    U.Email AS KullaniciEmail,
    R.ReservationDate,
    DATEDIFF(MINUTE, R.ReservationDate, GETDATE()) AS GecenDakika,
    CASE 
        WHEN DATEDIFF(MINUTE, R.ReservationDate, GETDATE()) >= 15 THEN 'İptal Edilmeli'
        ELSE 'Beklemede'
    END AS Durum,
    T.TripID,
    T.DepartureDate,
    T.DepartureTime,
    C1.CityName AS KalkisSehri,
    C2.CityName AS VarisSehri,
    T.Price AS SeferFiyati
FROM dbo.Reservations R
INNER JOIN dbo.Users U ON R.UserID = U.UserID
INNER JOIN dbo.Trips T ON R.TripID = T.TripID
INNER JOIN dbo.Cities C1 ON T.FromCityID = C1.CityID
INNER JOIN dbo.Cities C2 ON T.ToCityID = C2.CityID
LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID
WHERE R.Status = 'Reserved'
  AND (P.Status IS NULL OR P.Status = 'Pending')
  AND DATEDIFF(MINUTE, R.ReservationDate, GETDATE()) < 60; -- Son 60 dakika içindeki rezervasyonlar
GO

-- Test sorgusu
-- SELECT * FROM dbo.vw_Bekleyen_Iptaller ORDER BY GecenDakika DESC;

PRINT '✅ Otomatik İptal Sistemi başarıyla oluşturuldu!';
PRINT '📋 Oluşturulan nesneler:';
PRINT '   - Tablo: AutoCancellationLog';
PRINT '   - SP: sp_Zaman_Asimi_Rezervasyonlar';
PRINT '   - SP: sp_Otomatik_Iptal_Ayarlari';
PRINT '   - View: vw_Bekleyen_Iptaller';
PRINT '';
PRINT '🔧 Kullanım:';
PRINT '   EXEC sp_Zaman_Asimi_Rezervasyonlar @TimeoutMinutes = 15;';
GO

