-- =============================================
-- Bildirim Sistemini Sadece Email Kullanacak Şekilde Güncelle
-- SMS desteği kaldırıldı, sadece Email bildirimleri kullanılacak
-- =============================================
USE RayBusDB;
GO

-- 1. Önce mevcut SMS/Both bildirimlerini Email'e çevir (constraint hatası almamak için)
UPDATE dbo.NotificationQueue
SET NotificationMethod = 'Email'
WHERE NotificationMethod IN ('SMS', 'Both');
GO

-- 2. NotificationQueue tablosundaki CHECK constraint'i güncelle
-- (SMS ve Both seçeneklerini kaldır, sadece Email bırak)
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_NotificationQueue_Method')
BEGIN
    ALTER TABLE dbo.NotificationQueue
    DROP CONSTRAINT CK_NotificationQueue_Method;
END
GO

ALTER TABLE dbo.NotificationQueue
ADD CONSTRAINT CK_NotificationQueue_Method CHECK (NotificationMethod IN ('Email'));
GO

-- 3. Trigger'ları güncelle - sadece Email kullan
-- =============================================
-- trg_Rezervasyon_Bildirim - Sadece Email
-- =============================================
IF OBJECT_ID('dbo.trg_Rezervasyon_Bildirim', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Rezervasyon_Bildirim;
GO

CREATE TRIGGER dbo.trg_Rezervasyon_Bildirim
    ON dbo.Reservations
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
            'Reservation',
            'Email',
            'Rezervasyonunuz Oluşturuldu',
            'Sayın ' + U.FullName + ', ' + 
            'Rezervasyonunuz başarıyla oluşturuldu. Rezervasyon ID: ' + CAST(I.ReservationID AS NVARCHAR(10)) + 
            '. İyi yolculuklar dileriz!',
            I.ReservationID
        FROM inserted I
        INNER JOIN dbo.Users U ON I.UserID = U.UserID
        LEFT JOIN dbo.UserNotificationPreferences UNP ON I.UserID = UNP.UserID
        WHERE (UNP.ReservationNotifications = 1 OR UNP.ReservationNotifications IS NULL);
END;
GO

-- =============================================
-- trg_Odeme_Bildirim - Sadece Email
-- =============================================
IF OBJECT_ID('dbo.trg_Odeme_Bildirim', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Odeme_Bildirim;
GO

CREATE TRIGGER dbo.trg_Odeme_Bildirim
    ON dbo.Payments
    AFTER UPDATE
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
            R.UserID,
            'Payment',
            'Email',
            'Ödemeniz Tamamlandı',
            'Sayın ' + U.FullName + ', ' + 
            'Ödemeniz başarıyla tamamlandı. Tutar: ' + CAST(UPD.Amount AS NVARCHAR(20)) + ' ₺' +
            '. Bilet numaranız: ' + ISNULL(R.TicketNumber, CAST(R.ReservationID AS NVARCHAR(10))) + 
            '. İyi yolculuklar dileriz!',
            R.ReservationID
        FROM inserted UPD
        INNER JOIN deleted DEL ON UPD.PaymentID = DEL.PaymentID
        INNER JOIN dbo.Reservations R ON UPD.ReservationID = R.ReservationID
        INNER JOIN dbo.Users U ON R.UserID = U.UserID
        LEFT JOIN dbo.UserNotificationPreferences UNP ON R.UserID = UNP.UserID
        WHERE DEL.Status = 'Pending' 
          AND UPD.Status = 'Paid'
          AND (UNP.PaymentNotifications = 1 OR UNP.PaymentNotifications IS NULL);
END;
GO

-- =============================================
-- trg_Iptal_Bildirim - Sadece Email
-- =============================================
IF OBJECT_ID('dbo.trg_Iptal_Bildirim', 'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_Iptal_Bildirim;
GO

CREATE TRIGGER dbo.trg_Iptal_Bildirim
    ON dbo.Reservations
    AFTER UPDATE
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
            R.UserID,
            'Cancellation',
            'Email',
            'Rezervasyonunuz İptal Edildi',
            'Sayın ' + U.FullName + ', ' + 
            'Rezervasyonunuz iptal edildi. Rezervasyon ID: ' + CAST(R.ReservationID AS NVARCHAR(10)) + 
            '. İptal nedeni: ' + ISNULL(CR.ReasonText, 'Belirtilmedi') + 
            '. İade işlemleri hakkında bilgi için lütfen bizimle iletişime geçin.',
            R.ReservationID
        FROM inserted UPD
        INNER JOIN deleted DEL ON UPD.ReservationID = DEL.ReservationID
        INNER JOIN dbo.Reservations R ON UPD.ReservationID = R.ReservationID
        INNER JOIN dbo.Users U ON R.UserID = U.UserID
        LEFT JOIN dbo.CancellationReasons CR ON R.CancelReasonID = CR.ReasonID
        LEFT JOIN dbo.UserNotificationPreferences UNP ON R.UserID = UNP.UserID
        WHERE DEL.Status != 'Cancelled' 
          AND UPD.Status = 'Cancelled'
          AND (UNP.CancellationNotifications = 1 OR UNP.CancellationNotifications IS NULL);
END;
GO

-- =============================================
-- trg_Kullanici_Kayit_Bildirim - Sadece Email
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
            Message
        )
        SELECT
            I.UserID,
            'Registration',
            'Email',
            'RayBus''a Hoş Geldiniz!',
            'Sayın ' + I.FullName + ',' + CHAR(13) + CHAR(10) +
            'RayBus ailesine hoş geldiniz! Hesabınız başarıyla oluşturuldu.' + CHAR(13) + CHAR(10) +
            'Keyifli seyahatler dileriz.' + CHAR(13) + CHAR(10) +
            'RayBus Ekibi'
        FROM inserted I;
        -- RegistrationNotifications kolonu yok, bu yüzden tüm yeni kullanıcılara gönder
END;
GO

PRINT '✅ Bildirim sistemi sadece Email kullanacak şekilde güncellendi!';
PRINT '   - SMS ve Both seçenekleri kaldırıldı';
PRINT '   - Tüm trigger''lar sadece Email gönderecek şekilde güncellendi';
PRINT '   - Bekleyen SMS/Both bildirimleri Email''e çevrildi';
GO

