-- =============================================
-- Stored Procedure Kontrol Script'i
-- =============================================
-- Bu script mevcut stored procedure'lerin durumunu kontrol eder

USE [RayBus]
GO

-- 1. sp_Rezervasyon_Yap parametrelerini kontrol et
PRINT '============================================='
PRINT 'sp_Rezervasyon_Yap Parametreleri:'
PRINT '============================================='

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Rezervasyon_Yap')
BEGIN
    SELECT 
        p.parameter_id AS Sira,
        p.name AS ParametreAdi,
        t.name AS ParametreTipi,
        CASE 
            WHEN t.name = 'NVARCHAR' THEN CAST(p.max_length/2 AS VARCHAR) + ' karakter'
            WHEN t.name = 'DECIMAL' THEN t.name + '(' + CAST(p.precision AS VARCHAR) + ',' + CAST(p.scale AS VARCHAR) + ')'
            ELSE t.name
        END AS DetayliTip,
        CASE WHEN p.is_output = 1 THEN 'OUTPUT' ELSE 'INPUT' END AS Yon
    FROM sys.parameters p
    INNER JOIN sys.types t ON p.user_type_id = t.user_type_id
    WHERE p.object_id = OBJECT_ID('sp_Rezervasyon_Yap')
    ORDER BY p.parameter_id;
    
    PRINT ''
    PRINT 'Toplam Parametre Sayısı: ' + CAST((
        SELECT COUNT(*) 
        FROM sys.parameters 
        WHERE object_id = OBJECT_ID('sp_Rezervasyon_Yap')
    ) AS VARCHAR);
    
    -- @IslemTipi parametresinin varlığını kontrol et
    IF EXISTS (
        SELECT 1 
        FROM sys.parameters 
        WHERE object_id = OBJECT_ID('sp_Rezervasyon_Yap') 
        AND name = '@IslemTipi'
    )
    BEGIN
        PRINT '✅ @IslemTipi parametresi MEVCUT'
    END
    ELSE
    BEGIN
        PRINT '❌ @IslemTipi parametresi BULUNAMADI - Stored procedure güncellenmemiş!'
    END
END
ELSE
BEGIN
    PRINT '❌ sp_Rezervasyon_Yap stored procedure BULUNAMADI!'
END
GO

PRINT ''
PRINT '============================================='
PRINT 'sp_Odeme_Tamamla Kontrolü:'
PRINT '============================================='

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Odeme_Tamamla')
BEGIN
    PRINT '✅ sp_Odeme_Tamamla stored procedure MEVCUT'
    
    SELECT 
        p.parameter_id AS Sira,
        p.name AS ParametreAdi,
        t.name AS ParametreTipi
    FROM sys.parameters p
    INNER JOIN sys.types t ON p.user_type_id = t.user_type_id
    WHERE p.object_id = OBJECT_ID('sp_Odeme_Tamamla')
    ORDER BY p.parameter_id;
END
ELSE
BEGIN
    PRINT '❌ sp_Odeme_Tamamla stored procedure BULUNAMADI!'
END
GO

PRINT ''
PRINT '============================================='
PRINT 'Kontrol Tamamlandı'
PRINT '============================================='

