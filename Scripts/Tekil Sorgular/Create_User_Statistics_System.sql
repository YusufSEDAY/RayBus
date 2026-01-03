-- =============================================
-- Kullanıcı İstatistikleri Sistemi
-- Açıklama: Kullanıcıların seyahat istatistiklerini hesaplar
-- Tarih: 2024-12-15
-- =============================================

USE RayBusDB;
GO

-- =============================================
-- 1. Function: fn_Toplam_Harcama
-- Açıklama: Kullanıcının toplam harcamasını hesaplar
-- =============================================
IF OBJECT_ID('dbo.fn_Toplam_Harcama', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_Toplam_Harcama;
GO

CREATE FUNCTION dbo.fn_Toplam_Harcama(@UserID INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @ToplamHarcama DECIMAL(18,2) = 0;
    
    SELECT @ToplamHarcama = ISNULL(SUM(P.Amount), 0)
    FROM dbo.Payments P
    INNER JOIN dbo.Reservations R ON P.ReservationID = R.ReservationID
    WHERE R.UserID = @UserID
      AND P.Status = 'Paid';
    
    RETURN @ToplamHarcama;
END;
GO

-- =============================================
-- 2. Function: fn_Seyahat_Sayisi
-- Açıklama: Kullanıcının toplam seyahat sayısını hesaplar
-- =============================================
IF OBJECT_ID('dbo.fn_Seyahat_Sayisi', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_Seyahat_Sayisi;
GO

CREATE FUNCTION dbo.fn_Seyahat_Sayisi(@UserID INT)
RETURNS INT
AS
BEGIN
    DECLARE @SeyahatSayisi INT = 0;
    
    SELECT @SeyahatSayisi = COUNT(DISTINCT R.TripID)
    FROM dbo.Reservations R
    INNER JOIN dbo.Payments P ON R.ReservationID = P.ReservationID
    WHERE R.UserID = @UserID
      AND R.Status != 'Cancelled'
      AND P.Status = 'Paid';
    
    RETURN @SeyahatSayisi;
END;
GO

-- =============================================
-- 3. Function: fn_Ortalama_Seyahat_Fiyati
-- Açıklama: Kullanıcının ortalama seyahat fiyatını hesaplar
-- =============================================
IF OBJECT_ID('dbo.fn_Ortalama_Seyahat_Fiyati', 'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_Ortalama_Seyahat_Fiyati;
GO

CREATE FUNCTION dbo.fn_Ortalama_Seyahat_Fiyati(@UserID INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @OrtalamaFiyat DECIMAL(18,2) = 0;
    
    SELECT @OrtalamaFiyat = ISNULL(AVG(P.Amount), 0)
    FROM dbo.Payments P
    INNER JOIN dbo.Reservations R ON P.ReservationID = R.ReservationID
    WHERE R.UserID = @UserID
      AND R.Status != 'Cancelled'
      AND P.Status = 'Paid';
    
    RETURN @OrtalamaFiyat;
END;
GO

-- =============================================
-- 4. View: vw_Kullanici_Istatistikleri
-- Açıklama: Kullanıcı istatistiklerini toplu olarak gösterir
-- =============================================
IF OBJECT_ID('dbo.vw_Kullanici_Istatistikleri', 'V') IS NOT NULL
    DROP VIEW dbo.vw_Kullanici_Istatistikleri;
GO

CREATE VIEW dbo.vw_Kullanici_Istatistikleri
AS
SELECT 
    U.UserID,
    U.FullName AS KullaniciAdi,
    U.Email AS KullaniciEmail,
    
    -- Toplam Harcama
    dbo.fn_Toplam_Harcama(U.UserID) AS ToplamHarcama,
    
    -- Seyahat Sayıları
    dbo.fn_Seyahat_Sayisi(U.UserID) AS ToplamSeyahatSayisi,
    
    (SELECT COUNT(*) 
     FROM dbo.Reservations R
     INNER JOIN dbo.Payments P ON R.ReservationID = P.ReservationID
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     WHERE R.UserID = U.UserID 
       AND R.Status != 'Cancelled' 
       AND P.Status = 'Paid'
       AND T.DepartureDate >= GETDATE()) AS GelecekSeyahatSayisi,
    
    (SELECT COUNT(*) 
     FROM dbo.Reservations R
     INNER JOIN dbo.Payments P ON R.ReservationID = P.ReservationID
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     WHERE R.UserID = U.UserID 
       AND R.Status != 'Cancelled' 
       AND P.Status = 'Paid'
       AND T.DepartureDate < GETDATE()) AS GecmisSeyahatSayisi,
    
    -- Ortalama Fiyat
    dbo.fn_Ortalama_Seyahat_Fiyati(U.UserID) AS OrtalamaSeyahatFiyati,
    
    -- En Çok Gidilen Şehirler (Top 3)
    (SELECT TOP 1 C2.CityName
     FROM dbo.Reservations R
     INNER JOIN dbo.Payments P ON R.ReservationID = P.ReservationID
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     INNER JOIN dbo.Cities C2 ON T.ToCityID = C2.CityID
     WHERE R.UserID = U.UserID 
       AND R.Status != 'Cancelled' 
       AND P.Status = 'Paid'
     GROUP BY C2.CityName
     ORDER BY COUNT(*) DESC) AS EnCokGidilenSehir,
    
    -- Son Seyahat Tarihi
    (SELECT MAX(T.DepartureDate)
     FROM dbo.Reservations R
     INNER JOIN dbo.Payments P ON R.ReservationID = P.ReservationID
     INNER JOIN dbo.Trips T ON R.TripID = T.TripID
     WHERE R.UserID = U.UserID 
       AND R.Status != 'Cancelled' 
       AND P.Status = 'Paid') AS SonSeyahatTarihi,
    
    -- Toplam Rezervasyon Sayısı
    (SELECT COUNT(*) 
     FROM dbo.Reservations R
     WHERE R.UserID = U.UserID) AS ToplamRezervasyonSayisi,
    
    -- İptal Edilen Rezervasyon Sayısı
    (SELECT COUNT(*) 
     FROM dbo.Reservations R
     WHERE R.UserID = U.UserID 
       AND R.Status = 'Cancelled') AS IptalEdilenRezervasyonSayisi,
    
    -- Kayıt Tarihi
    U.CreatedAt AS KayitTarihi
    
FROM dbo.Users U
WHERE U.Status = 1;
GO

-- =============================================
-- 5. Stored Procedure: sp_Kullanici_Raporu
-- Açıklama: Kullanıcının detaylı raporunu getirir
-- =============================================
IF OBJECT_ID('dbo.sp_Kullanici_Raporu', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Kullanici_Raporu;
GO

CREATE PROCEDURE dbo.sp_Kullanici_Raporu
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Genel İstatistikler
    SELECT 
        'Genel İstatistikler' AS RaporTipi,
        KullaniciAdi,
        ToplamHarcama,
        ToplamSeyahatSayisi,
        GelecekSeyahatSayisi,
        GecmisSeyahatSayisi,
        OrtalamaSeyahatFiyati,
        EnCokGidilenSehir,
        SonSeyahatTarihi,
        ToplamRezervasyonSayisi,
        IptalEdilenRezervasyonSayisi
    FROM dbo.vw_Kullanici_Istatistikleri
    WHERE UserID = @UserID;
    
    -- Son 10 Seyahat
    SELECT TOP 10
        R.ReservationID,
        T.DepartureDate AS SeferTarihi,
        T.DepartureTime AS SeferSaati,
        C1.CityName AS KalkisSehri,
        C2.CityName AS VarisSehri,
        P.Amount AS OdenenTutar,
        R.Status AS RezervasyonDurumu,
        R.ReservationDate AS RezervasyonTarihi
    FROM dbo.Reservations R
    INNER JOIN dbo.Trips T ON R.TripID = T.TripID
    INNER JOIN dbo.Cities C1 ON T.FromCityID = C1.CityID
    INNER JOIN dbo.Cities C2 ON T.ToCityID = C2.CityID
    LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID AND P.Status = 'Paid'
    WHERE R.UserID = @UserID
    ORDER BY T.DepartureDate DESC, T.DepartureTime DESC;
    
    -- Aylık Harcama (Son 12 Ay)
    SELECT 
        YEAR(P.PaymentDate) AS Yil,
        MONTH(P.PaymentDate) AS Ay,
        SUM(P.Amount) AS AylikHarcama,
        COUNT(DISTINCT R.ReservationID) AS AylikSeyahatSayisi
    FROM dbo.Payments P
    INNER JOIN dbo.Reservations R ON P.ReservationID = R.ReservationID
    WHERE R.UserID = @UserID
      AND P.Status = 'Paid'
      AND P.PaymentDate >= DATEADD(MONTH, -12, GETDATE())
    GROUP BY YEAR(P.PaymentDate), MONTH(P.PaymentDate)
    ORDER BY Yil DESC, Ay DESC;
END;
GO

-- Test sorguları
-- SELECT * FROM dbo.vw_Kullanici_Istatistikleri WHERE UserID = 1;
-- EXEC sp_Kullanici_Raporu @UserID = 1;

PRINT '✅ Kullanıcı İstatistikleri Sistemi başarıyla oluşturuldu!';
PRINT '📋 Oluşturulan nesneler:';
PRINT '   - Function: fn_Toplam_Harcama';
PRINT '   - Function: fn_Seyahat_Sayisi';
PRINT '   - Function: fn_Ortalama_Seyahat_Fiyati';
PRINT '   - View: vw_Kullanici_Istatistikleri';
PRINT '   - SP: sp_Kullanici_Raporu';
PRINT '';
PRINT '🔧 Kullanım:';
PRINT '   SELECT * FROM dbo.vw_Kullanici_Istatistikleri WHERE UserID = 1;';
PRINT '   EXEC sp_Kullanici_Raporu @UserID = 1;';
GO

