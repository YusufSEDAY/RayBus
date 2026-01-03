-- =============================================
-- Tüm Yeni Özellikler - Toplu Çalıştırma
-- Açıklama: 4 yeni özelliğin tüm veritabanı nesnelerini oluşturur
-- Tarih: 2024-12-15
-- NOT: Bu dosya sadece bilgilendirme amaçlıdır.
--      Scriptleri tek tek çalıştırmanız gerekmektedir:
--      1. Create_AutoCancellation_System.sql
--      2. Create_User_Statistics_System.sql
--      3. Create_Ticket_PDF_System.sql
--      4. Create_Notification_System.sql
-- =============================================

USE RayBusDB;
GO

PRINT '========================================';
PRINT 'Yeni Özellikler - Çalıştırma Talimatları';
PRINT '========================================';
PRINT '';
PRINT '⚠️  NOT: Bu dosya sadece bilgilendirme amaçlıdır.';
PRINT '    Scriptleri aşağıdaki sırayla TEK TEK çalıştırın:';
PRINT '';
PRINT '1️⃣  Create_AutoCancellation_System.sql';
PRINT '2️⃣  Create_User_Statistics_System.sql';
PRINT '3️⃣  Create_Ticket_PDF_System.sql';
PRINT '4️⃣  Create_Notification_System.sql';
PRINT '';
PRINT '========================================';
PRINT 'Yüklenen Özellikler:';
PRINT '========================================';
PRINT '   1. Otomatik İptal Sistemi';
PRINT '   2. Kullanıcı İstatistikleri';
PRINT '   3. Bilet PDF İndirme';
PRINT '   4. Email/SMS Bildirimleri';
PRINT '';
PRINT '========================================';
PRINT 'Test Komutları:';
PRINT '========================================';
PRINT '';
PRINT '-- Otomatik iptal';
PRINT 'EXEC sp_Zaman_Asimi_Rezervasyonlar @TimeoutMinutes = 15;';
PRINT '';
PRINT '-- Kullanıcı istatistikleri';
PRINT 'SELECT * FROM vw_Kullanici_Istatistikleri WHERE UserID = 1;';
PRINT 'EXEC sp_Kullanici_Raporu @UserID = 1;';
PRINT '';
PRINT '-- Bilet bilgileri';
PRINT 'SELECT * FROM vw_Bilet_Detay WHERE ReservationID = 1;';
PRINT 'EXEC sp_Bilet_Bilgileri @ReservationID = 1;';
PRINT '';
PRINT '-- Bildirimler';
PRINT 'SELECT * FROM NotificationQueue WHERE Status = ''Pending'';';
PRINT 'EXEC sp_Bildirim_Kuyrugu_Isle @MaxProcessCount = 10;';
PRINT '';
GO

