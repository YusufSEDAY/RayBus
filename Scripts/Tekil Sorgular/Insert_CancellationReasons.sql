-- =============================================
-- CancellationReasons - Önceden Tanımlı İptal Nedenleri
-- =============================================
-- Bu script önceden tanımlı iptal nedenlerini ekler

USE [RayBusDB]
GO

-- Eğer tablo boşsa veya sıfırdan başlatmak istiyorsanız, önce mevcut kayıtları silin:
-- DELETE FROM dbo.CancellationReasons;
-- DBCC CHECKIDENT ('CancellationReasons', RESEED, 0);
-- GO

-- Önceden tanımlı iptal nedenlerini ekle
-- ID'ler otomatik artacak (1, 2, 3, 4, 5)

-- 1. Plan Değişti
IF NOT EXISTS (SELECT 1 FROM dbo.CancellationReasons WHERE ReasonID = 1)
BEGIN
    SET IDENTITY_INSERT dbo.CancellationReasons ON;
    INSERT INTO dbo.CancellationReasons (ReasonID, ReasonText)
    VALUES (1, 'Plan değişti');
    SET IDENTITY_INSERT dbo.CancellationReasons OFF;
END
GO

-- 2. Başka Sefer Seçtim
IF NOT EXISTS (SELECT 1 FROM dbo.CancellationReasons WHERE ReasonID = 2)
BEGIN
    SET IDENTITY_INSERT dbo.CancellationReasons ON;
    INSERT INTO dbo.CancellationReasons (ReasonID, ReasonText)
    VALUES (2, 'Başka sefer seçtim');
    SET IDENTITY_INSERT dbo.CancellationReasons OFF;
END
GO

-- 3. Yolculuk İptal Edildi
IF NOT EXISTS (SELECT 1 FROM dbo.CancellationReasons WHERE ReasonID = 3)
BEGIN
    SET IDENTITY_INSERT dbo.CancellationReasons ON;
    INSERT INTO dbo.CancellationReasons (ReasonID, ReasonText)
    VALUES (3, 'Yolculuk iptal edildi');
    SET IDENTITY_INSERT dbo.CancellationReasons OFF;
END
GO

-- 4. Fiyat Uygun Değil
IF NOT EXISTS (SELECT 1 FROM dbo.CancellationReasons WHERE ReasonID = 4)
BEGIN
    SET IDENTITY_INSERT dbo.CancellationReasons ON;
    INSERT INTO dbo.CancellationReasons (ReasonID, ReasonText)
    VALUES (4, 'Fiyat uygun değil');
    SET IDENTITY_INSERT dbo.CancellationReasons OFF;
END
GO

-- 5. Yanlış Sefer Seçtim
IF NOT EXISTS (SELECT 1 FROM dbo.CancellationReasons WHERE ReasonID = 5)
BEGIN
    SET IDENTITY_INSERT dbo.CancellationReasons ON;
    INSERT INTO dbo.CancellationReasons (ReasonID, ReasonText)
    VALUES (5, 'Yanlış sefer seçtim');
    SET IDENTITY_INSERT dbo.CancellationReasons OFF;
END
GO

-- 6. Diğer (Placeholder - Kullanıcılar özel neden ekleyecek)
-- Bu kayıt otomatik olarak eklenmeyecek, kullanıcı "Diğer" seçtiğinde özel neden eklenecek

PRINT '=============================================';
PRINT 'Önceden tanımlı iptal nedenleri eklendi:';
PRINT '  1. Plan değişti';
PRINT '  2. Başka sefer seçtim';
PRINT '  3. Yolculuk iptal edildi';
PRINT '  4. Fiyat uygun değil';
PRINT '  5. Yanlış sefer seçtim';
PRINT '  6. Diğer (Kullanıcı özel neden girecek)';
PRINT '=============================================';
GO

