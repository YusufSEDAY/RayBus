-- =============================================
-- Email/SMS Bildirimleri Sistemi
-- Açıklama: Otomatik bildirim gönderme sistemi
-- Tarih: 2024-12-15
-- =============================================

USE RayBusDB;
GO

-- =============================================
-- 1. NotificationQueue Tablosu
-- =============================================
IF OBJECT_ID('dbo.NotificationQueue', 'U') IS NOT NULL
    DROP TABLE dbo.NotificationQueue;
GO

CREATE TABLE dbo.NotificationQueue (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    NotificationType NVARCHAR(50) NOT NULL, -- 'Reservation', 'Payment', 'Cancellation', 'Reminder', 'Registration'
    NotificationMethod NVARCHAR(20) NOT NULL, -- 'Email', 'SMS', 'Both'
    Subject NVARCHAR(200) NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending' NOT NULL, -- 'Pending', 'Sent', 'Failed'
    CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    SentAt DATETIME2 NULL,
    RetryCount INT DEFAULT 0 NOT NULL,
    ErrorMessage NVARCHAR(500) NULL,
    RelatedReservationID INT NULL,
    CONSTRAINT FK_NotificationQueue_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_NotificationQueue_Reservations FOREIGN KEY (RelatedReservationID) REFERENCES Reservations(ReservationID),
    CONSTRAINT CK_NotificationQueue_Status CHECK (Status IN ('Pending', 'Sent', 'Failed')),
    CONSTRAINT CK_NotificationQueue_Method CHECK (NotificationMethod IN ('Email', 'SMS', 'Both'))
);
GO

CREATE INDEX IX_NotificationQueue_Status ON dbo.NotificationQueue(Status);
CREATE INDEX IX_NotificationQueue_CreatedAt ON dbo.NotificationQueue(CreatedAt);
CREATE INDEX IX_NotificationQueue_UserID ON dbo.NotificationQueue(UserID);
GO

-- =============================================
-- 2. UserNotificationPreferences Tablosu
-- Açıklama: Kullanıcı bildirim tercihleri
-- =============================================
IF OBJECT_ID('dbo.UserNotificationPreferences', 'U') IS NOT NULL
    DROP TABLE dbo.UserNotificationPreferences;
GO

CREATE TABLE dbo.UserNotificationPreferences (
    PreferenceID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL UNIQUE,
    EmailNotifications BIT DEFAULT 1 NOT NULL,
    SMSNotifications BIT DEFAULT 0 NOT NULL,
    ReservationNotifications BIT DEFAULT 1 NOT NULL,
    PaymentNotifications BIT DEFAULT 1 NOT NULL,
    CancellationNotifications BIT DEFAULT 1 NOT NULL,
    ReminderNotifications BIT DEFAULT 1 NOT NULL,
    UpdatedAt DATETIME2 DEFAULT SYSUTCDATETIME() NOT NULL,
    CONSTRAINT FK_UserNotificationPreferences_Users FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- =============================================
-- 3. Trigger: trg_Rezervasyon_Bildirim
-- Açıklama: Rezervasyon oluşturulduğunda bildirim ekler
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
        CASE 
            WHEN UNP.EmailNotifications = 1 AND UNP.SMSNotifications = 1 THEN 'Both'
            WHEN UNP.EmailNotifications = 1 THEN 'Email'
            WHEN UNP.SMSNotifications = 1 THEN 'SMS'
            ELSE 'Email' -- Varsayılan
        END,
        'Rezervasyonunuz Oluşturuldu',
        'Sayın ' + U.FullName + ', ' + 
        'Rezervasyonunuz başarıyla oluşturuldu. Rezervasyon ID: ' + CAST(I.ReservationID AS NVARCHAR(10)) + 
        '. Ödeme yapmak için rezervasyonlarınız sayfasını ziyaret edin.',
        I.ReservationID
    FROM inserted I
    INNER JOIN dbo.Users U ON I.UserID = U.UserID
    LEFT JOIN dbo.UserNotificationPreferences UNP ON I.UserID = UNP.UserID
    WHERE (UNP.ReservationNotifications = 1 OR UNP.ReservationNotifications IS NULL);
END;
GO

-- =============================================
-- 4. Trigger: trg_Odeme_Bildirim
-- Açıklama: Ödeme tamamlandığında bildirim ekler
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
    
    -- Sadece status 'Pending'den 'Paid'e değiştiğinde
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
        CASE 
            WHEN UNP.EmailNotifications = 1 AND UNP.SMSNotifications = 1 THEN 'Both'
            WHEN UNP.EmailNotifications = 1 THEN 'Email'
            WHEN UNP.SMSNotifications = 1 THEN 'SMS'
            ELSE 'Email'
        END,
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
-- 5. Trigger: trg_Iptal_Bildirim
-- Açıklama: Rezervasyon iptal edildiğinde bildirim ekler
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
    
    -- Sadece status 'Cancelled' olarak değiştiğinde
    INSERT INTO dbo.NotificationQueue (
        UserID,
        NotificationType,
        NotificationMethod,
        Subject,
        Message,
        RelatedReservationID
    )
    SELECT 
        UPD.UserID,
        'Cancellation',
        CASE 
            WHEN UNP.EmailNotifications = 1 AND UNP.SMSNotifications = 1 THEN 'Both'
            WHEN UNP.EmailNotifications = 1 THEN 'Email'
            WHEN UNP.SMSNotifications = 1 THEN 'SMS'
            ELSE 'Email'
        END,
        'Rezervasyonunuz İptal Edildi',
        'Sayın ' + U.FullName + ', ' + 
        'Rezervasyonunuz (ID: ' + CAST(UPD.ReservationID AS NVARCHAR(10)) + ') iptal edilmiştir.' +
        CASE 
            WHEN CR.ReasonText IS NOT NULL THEN ' İptal nedeni: ' + CR.ReasonText
            ELSE ''
        END,
        UPD.ReservationID
    FROM inserted UPD
    INNER JOIN deleted DEL ON UPD.ReservationID = DEL.ReservationID
    INNER JOIN dbo.Users U ON UPD.UserID = U.UserID
    LEFT JOIN dbo.UserNotificationPreferences UNP ON UPD.UserID = UNP.UserID
    LEFT JOIN dbo.CancellationReasons CR ON UPD.CancelReasonID = CR.ReasonID
    WHERE DEL.Status != 'Cancelled' 
      AND UPD.Status = 'Cancelled'
      AND (UNP.CancellationNotifications = 1 OR UNP.CancellationNotifications IS NULL);
END;
GO

-- =============================================
-- 6. Stored Procedure: sp_Bildirim_Gonder
-- Açıklama: Bildirim gönderme (backend'den çağrılır)
-- =============================================
IF OBJECT_ID('dbo.sp_Bildirim_Gonder', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Bildirim_Gonder;
GO

CREATE PROCEDURE dbo.sp_Bildirim_Gonder
    @UserID INT,
    @NotificationType NVARCHAR(50),
    @NotificationMethod NVARCHAR(20) = 'Email',
    @Subject NVARCHAR(200),
    @Message NVARCHAR(MAX),
    @RelatedReservationID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NotificationID INT;
    
    -- Bildirim tercihlerini kontrol et
    DECLARE @EmailEnabled BIT = 1;
    DECLARE @SMSEnabled BIT = 0;
    DECLARE @TypeEnabled BIT = 1;
    
    SELECT 
        @EmailEnabled = ISNULL(EmailNotifications, 1),
        @SMSEnabled = ISNULL(SMSNotifications, 0),
        @TypeEnabled = CASE @NotificationType
            WHEN 'Reservation' THEN ISNULL(ReservationNotifications, 1)
            WHEN 'Payment' THEN ISNULL(PaymentNotifications, 1)
            WHEN 'Cancellation' THEN ISNULL(CancellationNotifications, 1)
            WHEN 'Reminder' THEN ISNULL(ReminderNotifications, 1)
            ELSE 1
        END
    FROM dbo.UserNotificationPreferences
    WHERE UserID = @UserID;
    
    IF @TypeEnabled = 0
    BEGIN
        SELECT 
            -1 AS NotificationID,
            'Kullanıcı bu bildirim tipini devre dışı bırakmış' AS Mesaj;
        RETURN;
    END
    
    -- Bildirim metodunu ayarla
    IF @NotificationMethod = 'Both' AND (@EmailEnabled = 0 OR @SMSEnabled = 0)
    BEGIN
        IF @EmailEnabled = 1
            SET @NotificationMethod = 'Email';
        ELSE IF @SMSEnabled = 1
            SET @NotificationMethod = 'SMS';
        ELSE
        BEGIN
            SELECT 
                -1 AS NotificationID,
                'Kullanıcının aktif bildirim yöntemi yok' AS Mesaj;
            RETURN;
        END
    END
    
    -- Bildirimi kuyruğa ekle
    INSERT INTO dbo.NotificationQueue (
        UserID,
        NotificationType,
        NotificationMethod,
        Subject,
        Message,
        RelatedReservationID
    )
    VALUES (
        @UserID,
        @NotificationType,
        @NotificationMethod,
        @Subject,
        @Message,
        @RelatedReservationID
    );
    
    SET @NotificationID = SCOPE_IDENTITY();
    
    SELECT 
        @NotificationID AS NotificationID,
        'Bildirim kuyruğa eklendi' AS Mesaj;
END;
GO

-- =============================================
-- 7. Stored Procedure: sp_Bildirim_Kuyrugu_Isle
-- Açıklama: Kuyruktaki bildirimleri işler (backend'den çağrılır)
-- =============================================
IF OBJECT_ID('dbo.sp_Bildirim_Kuyrugu_Isle', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Bildirim_Kuyrugu_Isle;
GO

CREATE PROCEDURE dbo.sp_Bildirim_Kuyrugu_Isle
    @MaxProcessCount INT = 100 -- Bir seferde işlenecek maksimum bildirim sayısı
AS
BEGIN
    SET NOCOUNT ON;
    
    -- İşlenecek bildirimleri getir
    SELECT TOP (@MaxProcessCount)
        NotificationID,
        UserID,
        NotificationType,
        NotificationMethod,
        Subject,
        Message,
        RelatedReservationID
    FROM dbo.NotificationQueue
    WHERE Status = 'Pending'
      AND RetryCount < 3 -- Maksimum 3 deneme
    ORDER BY CreatedAt ASC;
    
    -- NOT: Backend bu bildirimleri alıp gönderecek ve status'u güncelleyecek
    -- Bu SP sadece işlenecek bildirimleri listeler
END;
GO

-- =============================================
-- 8. Stored Procedure: sp_Bildirim_Durum_Guncelle
-- Açıklama: Bildirim durumunu günceller (backend'den çağrılır)
-- =============================================
IF OBJECT_ID('dbo.sp_Bildirim_Durum_Guncelle', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_Bildirim_Durum_Guncelle;
GO

CREATE PROCEDURE dbo.sp_Bildirim_Durum_Guncelle
    @NotificationID INT,
    @Status NVARCHAR(20), -- 'Sent', 'Failed'
    @ErrorMessage NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.NotificationQueue
    SET 
        Status = @Status,
        SentAt = CASE WHEN @Status = 'Sent' THEN GETDATE() ELSE SentAt END,
        RetryCount = RetryCount + 1,
        ErrorMessage = @ErrorMessage
    WHERE NotificationID = @NotificationID;
    
    SELECT 
        @NotificationID AS NotificationID,
        'Durum güncellendi' AS Mesaj;
END;
GO

-- Test sorguları
-- SELECT * FROM dbo.NotificationQueue WHERE Status = 'Pending' ORDER BY CreatedAt;
-- EXEC sp_Bildirim_Gonder @UserID = 1, @NotificationType = 'Reservation', @Subject = 'Test', @Message = 'Test mesajı';
-- EXEC sp_Bildirim_Kuyrugu_Isle @MaxProcessCount = 10;

-- =============================================
-- 7. Trigger: trg_Kullanici_Kayit_Bildirim
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
        'Email', -- Varsayılan olarak email gönder
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

PRINT '✅ Email/SMS Bildirimleri Sistemi başarıyla oluşturuldu!';
PRINT '📋 Oluşturulan nesneler:';
PRINT '   - Tablo: NotificationQueue';
PRINT '   - Tablo: UserNotificationPreferences';
PRINT '   - Trigger: trg_Rezervasyon_Bildirim';
PRINT '   - Trigger: trg_Odeme_Bildirim';
PRINT '   - Trigger: trg_Iptal_Bildirim';
PRINT '   - Trigger: trg_Kullanici_Kayit_Bildirim';
PRINT '   - SP: sp_Bildirim_Gonder';
PRINT '   - SP: sp_Bildirim_Kuyrugu_Isle';
PRINT '   - SP: sp_Bildirim_Durum_Guncelle';
PRINT '';
PRINT '🔧 Kullanım:';
PRINT '   EXEC sp_Bildirim_Gonder @UserID = 1, @NotificationType = ''Reservation'', @Subject = ''Test'', @Message = ''Test mesajı'';';
PRINT '   EXEC sp_Bildirim_Kuyrugu_Isle @MaxProcessCount = 10;';
GO

