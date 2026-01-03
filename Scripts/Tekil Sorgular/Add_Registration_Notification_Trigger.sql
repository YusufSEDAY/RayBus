-- =============================================
-- Kayıt Bildirimi Trigger'ı Ekleme
-- Açıklama: Yeni kullanıcı kayıt olduğunda hoş geldin email'i gönderir
-- =============================================

USE RayBusDB;
GO

-- NotificationType'a 'Registration' ekle (eğer yoksa)
-- Not: Tablo zaten oluşturulmuşsa bu sadece bilgilendirme amaçlı

-- =============================================
-- Trigger: trg_Kullanici_Kayit_Bildirim
-- Açıklama: Yeni kullanıcı kayıt olduğunda hoş geldin bildirimi ekler
-- =============================================
IF OBJECT_ID('dbo.trg_Kullanici_Kayit_Bildirim', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Kullanici_Kayit_Bildirim;
GO

CREATE TRIGGER dbo.trg_Kullanici_Kayit_Bildirim
ON dbo.Users
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.NotificationQueue (
        UserID,
        NotificationType,
        NotificationMethod,
        Subject,
        Message,
        RelatedReservationID
    )
    SELECT 
        I.UserID,
        'Registration', -- Yeni bildirim tipi
        'Email', -- Varsayılan olarak email gönder (kayıt bildirimi için özel)
        'RayBus''a Hoş Geldiniz! 🎉',
        'Sayın ' + I.FullName + ', ' + 
        'RayBus ailesine katıldığınız için teşekkür ederiz! ' +
        'Artık tren ve otobüs biletlerinizi kolayca rezerve edebilirsiniz. ' +
        'İyi yolculuklar dileriz! 🚌🚄',
        NULL -- Kayıt için rezervasyon yok
    FROM inserted I
    WHERE I.Status = 1; -- Sadece aktif kullanıcılar için
END;
GO

PRINT '✅ Kayıt bildirimi trigger''ı başarıyla oluşturuldu!';
PRINT '📋 Yeni kullanıcılar kayıt olduğunda otomatik olarak hoş geldin email''i gönderilecek.';
GO

