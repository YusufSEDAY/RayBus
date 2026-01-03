-- =============================================
-- TÜM SCHEMA NESNELERİNİ UYGULA
-- =============================================
-- Bu dosya tüm organize edilmiş script'leri sırayla çalıştırır
-- Tarih: 2024-12-19
-- =============================================
-- 
-- ÇALIŞTIRMA SIRASI:
-- 1. 01_CREATE_SCHEMAS_AND_MIGRATE.sql (Schema'ları oluştur ve nesneleri taşı)
-- 2. 07_ALL_FUNCTIONS_SCHEMA.sql (Function'ları oluştur - view'lar bunları kullanır)
-- 3. 04_ALL_VIEWS_SCHEMA.sql (View'ları oluştur)
-- 4. 05_ALL_STORED_PROCEDURES_SCHEMA.sql (Stored procedure'leri oluştur)
-- 5. 06_ALL_TRIGGERS_SCHEMA.sql (Trigger'ları oluştur)
-- =============================================

USE [RayBusDB]
GO

PRINT '========================================';
PRINT 'TÜM SCHEMA NESNELERİ UYGULANIYOR...';
PRINT '========================================';
PRINT '';
PRINT 'NOT: Bu dosya sadece bilgilendirme amaçlıdır.';
PRINT '     Lütfen script''leri sırayla çalıştırın:';
PRINT '';
PRINT '1. 01_CREATE_SCHEMAS_AND_MIGRATE.sql';
PRINT '2. 07_ALL_FUNCTIONS_SCHEMA.sql';
PRINT '3. 04_ALL_VIEWS_SCHEMA.sql';
PRINT '4. 05_ALL_STORED_PROCEDURES_SCHEMA.sql';
PRINT '5. 06_ALL_TRIGGERS_SCHEMA.sql';
PRINT '';
PRINT '========================================';
GO

