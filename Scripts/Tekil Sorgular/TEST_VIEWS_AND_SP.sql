-- =============================================
-- VIEW VE STORED PROCEDURE KONTROLÜ
-- =============================================
-- Bu script view'ların ve stored procedure'lerin
-- doğru schema'larda oluşturulup oluşturulmadığını kontrol eder
-- =============================================

USE [RayBusDB]
GO

PRINT '========================================';
PRINT 'VIEW VE STORED PROCEDURE KONTROLÜ';
PRINT '========================================';
PRINT '';

-- View'ları kontrol et
PRINT '1. VIEW KONTROLÜ:';
PRINT '';

SELECT 
    'View' AS NesneTipi,
    SCHEMA_NAME(schema_id) AS SchemaAdi,
    name AS NesneAdi,
    CASE 
        WHEN SCHEMA_NAME(schema_id) = 'report' THEN '✅ Doğru'
        ELSE '❌ Yanlış Schema'
    END AS Durum
FROM sys.views
WHERE name LIKE 'vw_%'
ORDER BY SCHEMA_NAME(schema_id), name;

PRINT '';

-- Stored Procedure'leri kontrol et
PRINT '2. STORED PROCEDURE KONTROLÜ:';
PRINT '';

SELECT 
    'Stored Procedure' AS NesneTipi,
    SCHEMA_NAME(schema_id) AS SchemaAdi,
    name AS NesneAdi,
    CASE 
        WHEN SCHEMA_NAME(schema_id) = 'proc' OR SCHEMA_NAME(schema_id) = '[proc]' THEN '✅ Doğru'
        ELSE '❌ Yanlış Schema'
    END AS Durum
FROM sys.procedures
WHERE name LIKE 'sp_%'
ORDER BY SCHEMA_NAME(schema_id), name;

PRINT '';

-- View'ları test et
PRINT '3. VIEW TEST:';
PRINT '';

-- Admin Dashboard View
PRINT 'Testing report.vw_Admin_Dashboard_Istatistikleri...';
SELECT TOP 1 * FROM report.vw_Admin_Dashboard_Istatistikleri;
PRINT '✅ vw_Admin_Dashboard_Istatistikleri çalışıyor';
PRINT '';

-- Şirket İstatistikleri View
PRINT 'Testing report.vw_Sirket_Istatistikleri...';
SELECT TOP 1 * FROM report.vw_Sirket_Istatistikleri;
PRINT '✅ vw_Sirket_Istatistikleri çalışıyor';
PRINT '';

-- Stored Procedure'leri test et
PRINT '4. STORED PROCEDURE TEST:';
PRINT '';

-- Kullanıcı Kayıt SP
PRINT 'Testing [proc].sp_Kullanici_Kayit...';
IF OBJECT_ID('[proc].sp_Kullanici_Kayit', 'P') IS NOT NULL
    PRINT '✅ [proc].sp_Kullanici_Kayit mevcut';
ELSE
    PRINT '❌ [proc].sp_Kullanici_Kayit bulunamadı';
PRINT '';

-- Şirket İstatistikleri SP
PRINT 'Testing [proc].sp_Sirket_Istatistikleri_Getir...';
IF OBJECT_ID('[proc].sp_Sirket_Istatistikleri_Getir', 'P') IS NOT NULL
    PRINT '✅ [proc].sp_Sirket_Istatistikleri_Getir mevcut';
ELSE
    PRINT '❌ [proc].sp_Sirket_Istatistikleri_Getir bulunamadı';
PRINT '';

PRINT '========================================';
PRINT 'KONTROL TAMAMLANDI';
PRINT '========================================';
GO

