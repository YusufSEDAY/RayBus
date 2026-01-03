-- =============================================
-- TÜM FUNCTION'LAR - SCHEMA ORGANİZE EDİLMİŞ VERSİYON
-- =============================================
-- Bu dosya tüm function'ları func schema'sında oluşturur
-- İçeriklerinde app ve log schema referansları kullanılır
-- Tarih: 2024-12-19
-- =============================================

USE [RayBusDB]
GO

PRINT '========================================';
PRINT 'TÜM FUNCTION''LAR OLUŞTURULUYOR...';
PRINT '========================================';
PRINT '';

-- =============================================
-- 1. fn_Toplam_Harcama
-- =============================================
PRINT '1. fn_Toplam_Harcama oluşturuluyor...';

IF OBJECT_ID('[func].fn_Toplam_Harcama', 'FN') IS NOT NULL
    DROP FUNCTION [func].fn_Toplam_Harcama;
GO

CREATE FUNCTION [func].fn_Toplam_Harcama(@UserID INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @ToplamHarcama DECIMAL(18,2) = 0;
    
    SELECT @ToplamHarcama = ISNULL(SUM(P.Amount), 0)
    FROM app.Payments P
    INNER JOIN app.Reservations R ON P.ReservationID = R.ReservationID
    WHERE R.UserID = @UserID
      AND P.Status = 'Completed';
    
    RETURN @ToplamHarcama;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 2. fn_Seyahat_Sayisi
-- =============================================
PRINT '2. fn_Seyahat_Sayisi oluşturuluyor...';

IF OBJECT_ID('[func].fn_Seyahat_Sayisi', 'FN') IS NOT NULL
    DROP FUNCTION [func].fn_Seyahat_Sayisi;
GO

CREATE FUNCTION [func].fn_Seyahat_Sayisi(@UserID INT)
RETURNS INT
AS
BEGIN
    DECLARE @SeyahatSayisi INT = 0;
    
    SELECT @SeyahatSayisi = COUNT(DISTINCT R.TripID)
    FROM app.Reservations R
    INNER JOIN app.Payments P ON R.ReservationID = P.ReservationID
    WHERE R.UserID = @UserID
      AND R.Status != 'Cancelled'
      AND P.Status = 'Completed';
    
    RETURN @SeyahatSayisi;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 3. fn_Ortalama_Seyahat_Fiyati
-- =============================================
PRINT '3. fn_Ortalama_Seyahat_Fiyati oluşturuluyor...';

IF OBJECT_ID('[func].fn_Ortalama_Seyahat_Fiyati', 'FN') IS NOT NULL
    DROP FUNCTION [func].fn_Ortalama_Seyahat_Fiyati;
GO

CREATE FUNCTION [func].fn_Ortalama_Seyahat_Fiyati(@UserID INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @OrtalamaFiyat DECIMAL(18,2) = 0;
    
    SELECT @OrtalamaFiyat = ISNULL(AVG(P.Amount), 0)
    FROM app.Payments P
    INNER JOIN app.Reservations R ON P.ReservationID = R.ReservationID
    WHERE R.UserID = @UserID
      AND R.Status != 'Cancelled'
      AND P.Status = 'Completed';
    
    RETURN @OrtalamaFiyat;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- TAMAMLANDI
-- =============================================
PRINT '========================================';
PRINT 'TÜM FUNCTION''LAR BAŞARIYLA OLUŞTURULDU!';
PRINT '========================================';
PRINT '';
PRINT 'Oluşturulan Function''lar (func schema):';
PRINT '  1. fn_Toplam_Harcama';
PRINT '  2. fn_Seyahat_Sayisi';
PRINT '  3. fn_Ortalama_Seyahat_Fiyati';
PRINT '';
PRINT 'Test sorguları:';
PRINT '  SELECT [func].fn_Toplam_Harcama(1);';
PRINT '  SELECT [func].fn_Seyahat_Sayisi(1);';
PRINT '  SELECT [func].fn_Ortalama_Seyahat_Fiyati(1);';
PRINT '';
GO

