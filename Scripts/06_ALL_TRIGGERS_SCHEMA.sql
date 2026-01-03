-- =============================================
-- TÜM TRIGGER'LAR - SCHEMA ORGANİZE EDİLMİŞ VERSİYON
-- =============================================
-- Bu dosya tüm trigger'ları app schema'sında oluşturur
-- NOT: SQL Server trigger'ların hedef tablo ile aynı schema'da olmasını zorunlu kılar
-- İçeriklerinde app ve log schema referansları kullanılır
-- Tarih: 2024-12-19
-- =============================================

USE [RayBusDB]
GO

PRINT '========================================';
PRINT 'TÜM TRIGGER''LAR OLUŞTURULUYOR...';
PRINT '========================================';
PRINT '';

-- =============================================
-- 1. trg_Rezervasyon_Sonrasi_Koltuk_Guncelle
-- =============================================
PRINT '1. trg_Rezervasyon_Sonrasi_Koltuk_Guncelle oluşturuluyor...';

IF OBJECT_ID('app.trg_Rezervasyon_Sonrasi_Koltuk_Guncelle', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Rezervasyon_Sonrasi_Koltuk_Guncelle;
GO

CREATE TRIGGER app.trg_Rezervasyon_Sonrasi_Koltuk_Guncelle
ON app.Reservations
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        UPDATE app.TripSeats
        SET IsReserved = 1,
            ReservedAt = SYSUTCDATETIME()
        FROM app.TripSeats TS
        INNER JOIN inserted i ON TS.TripID = i.TripID AND TS.SeatID = i.SeatID;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 2. trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar
-- =============================================
PRINT '2. trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar oluşturuluyor...';

IF OBJECT_ID('app.trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar;
GO

CREATE TRIGGER app.trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar
ON app.Reservations
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        IF UPDATE(Status)
        BEGIN
            UPDATE app.TripSeats
            SET IsReserved = 0,
                ReservedAt = NULL
            FROM app.TripSeats TS
            INNER JOIN inserted i ON TS.TripID = i.TripID AND TS.SeatID = i.SeatID
            INNER JOIN deleted d ON i.ReservationID = d.ReservationID
            WHERE i.Status = 'Cancelled'
              AND d.Status <> 'Cancelled';
        END
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @HataMesaji NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@HataMesaji, 16, 1);
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 3. trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur
-- =============================================
PRINT '3. trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur oluşturuluyor...';

IF OBJECT_ID('app.trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur;
GO

CREATE TRIGGER app.trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur
ON app.Trips
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Yeni eklenen seferin VehicleID'sini al
        -- İlgili araçtaki tüm koltukları bul
        -- Her koltuk için TripSeats kaydı oluştur
        INSERT INTO app.TripSeats (TripID, SeatID, IsReserved)
        SELECT 
            i.TripID,
            S.SeatID,
            0 -- Başlangıçta boş
        FROM inserted i
        INNER JOIN app.Vehicles V ON i.VehicleID = V.VehicleID
        INNER JOIN app.Seats S ON V.VehicleID = S.VehicleID
        WHERE S.IsActive = 1;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 4. trg_Sefer_Guncellendiginde_Log_Tut
-- =============================================
PRINT '4. trg_Sefer_Guncellendiginde_Log_Tut oluşturuluyor...';

IF OBJECT_ID('app.trg_Sefer_Guncellendiginde_Log_Tut', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Sefer_Guncellendiginde_Log_Tut;
GO

CREATE TRIGGER app.trg_Sefer_Guncellendiginde_Log_Tut
ON app.Trips
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Fiyat Değişikliği
    IF UPDATE(Price)
    BEGIN
        INSERT INTO log.TripLogs (TripID, ColumnName, OldValue, NewValue, ChangedAt)
        SELECT 
            i.TripID, 
            'Price', 
            CAST(d.Price AS NVARCHAR(50)), 
            CAST(i.Price AS NVARCHAR(50)), 
            SYSUTCDATETIME()
        FROM inserted i
        INNER JOIN deleted d ON i.TripID = d.TripID
        WHERE i.Price <> d.Price;
    END

    -- 2. Durum Değişikliği
    IF UPDATE(Status)
    BEGIN
        INSERT INTO log.TripLogs (TripID, ColumnName, OldValue, NewValue, ChangedAt)
        SELECT 
            i.TripID, 
            'Status', 
            CAST(d.Status AS NVARCHAR(50)), 
            CAST(i.Status AS NVARCHAR(50)), 
            SYSUTCDATETIME()
        FROM inserted i
        INNER JOIN deleted d ON i.TripID = d.TripID
        WHERE i.Status <> d.Status;
    END

    -- 3. Kalkış Tarihi Değişikliği
    IF UPDATE(DepartureDate)
    BEGIN
        INSERT INTO log.TripLogs (TripID, ColumnName, OldValue, NewValue, ChangedAt)
        SELECT 
            i.TripID, 
            'DepartureDate', 
            CAST(d.DepartureDate AS NVARCHAR(50)), 
            CAST(i.DepartureDate AS NVARCHAR(50)), 
            SYSUTCDATETIME()
        FROM inserted i
        INNER JOIN deleted d ON i.TripID = d.TripID
        WHERE i.DepartureDate <> d.DepartureDate;
    END
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 5. trg_Odeme_Islemleri_Logla
-- =============================================
PRINT '5. trg_Odeme_Islemleri_Logla oluşturuluyor...';

IF OBJECT_ID('app.trg_Odeme_Islemleri_Logla', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Odeme_Islemleri_Logla;
GO

CREATE TRIGGER app.trg_Odeme_Islemleri_Logla
ON app.Payments
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. DURUM: Yeni Bir Ödeme Eklendi mi? (INSERT işlemi)
    IF EXISTS (SELECT * FROM inserted) AND NOT EXISTS (SELECT * FROM deleted)
    BEGIN
        INSERT INTO log.PaymentLogs (PaymentID, Action, NewStatus, Description)
        SELECT 
            i.PaymentID, 
            'Olusturuldu',
            i.Status, 
            'Yeni ödeme kaydı oluşturuldu. Tutar: ' + CAST(i.Amount AS NVARCHAR(20))
        FROM inserted i;
    END

    -- 2. DURUM: Ödeme Güncellendi mi? (UPDATE işlemi)
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        IF UPDATE(Status)
        BEGIN
            INSERT INTO log.PaymentLogs (PaymentID, Action, OldStatus, NewStatus, Description)
            SELECT 
                i.PaymentID, 
                'DurumDegisikligi',
                d.Status,
                i.Status,
                'Ödeme durumu güncellendi.'
            FROM inserted i
            INNER JOIN deleted d ON i.PaymentID = d.PaymentID
            WHERE i.Status <> d.Status;
        END
    END
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 6. trg_Bilet_Numarasi
-- =============================================
PRINT '6. trg_Bilet_Numarasi oluşturuluyor...';

IF OBJECT_ID('app.trg_Bilet_Numarasi', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Bilet_Numarasi;
GO

CREATE TRIGGER app.trg_Bilet_Numarasi
ON app.Reservations
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ReservationID INT;
    DECLARE @BiletNumarasi NVARCHAR(50);
    DECLARE @Tarih NVARCHAR(8) = FORMAT(GETDATE(), 'yyyyMMdd');
    DECLARE @Saat NVARCHAR(6) = FORMAT(GETDATE(), 'HHmmss');
    DECLARE @RastgeleSayi INT;
    DECLARE @SayiStr NVARCHAR(5);
    DECLARE @Counter INT = 0;
    
    DECLARE reservation_cursor CURSOR FOR
    SELECT ReservationID
    FROM inserted
    WHERE TicketNumber IS NULL;
    
    OPEN reservation_cursor;
    FETCH NEXT FROM reservation_cursor INTO @ReservationID;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Counter = 0;
        
        WHILE @Counter < 100
        BEGIN
            SET @RastgeleSayi = ABS(CHECKSUM(NEWID())) % 99999;
            SET @SayiStr = RIGHT('00000' + CAST(@RastgeleSayi AS NVARCHAR(5)), 5);
            SET @BiletNumarasi = 'RB-' + @Tarih + '-' + @Saat + '-' + @SayiStr;
            
            IF NOT EXISTS (SELECT 1 FROM app.Reservations WHERE TicketNumber = @BiletNumarasi)
            BEGIN
                UPDATE app.Reservations
                SET TicketNumber = @BiletNumarasi
                WHERE ReservationID = @ReservationID;
                
                BREAK;
            END
            
            SET @Counter = @Counter + 1;
        END
        
        IF @Counter >= 100
        BEGIN
            SET @BiletNumarasi = 'RB-' + @Tarih + '-' + @Saat + '-' + RIGHT('00000' + CAST(@ReservationID AS NVARCHAR(10)), 5);
            
            UPDATE app.Reservations
            SET TicketNumber = @BiletNumarasi
            WHERE ReservationID = @ReservationID;
        END
        
        FETCH NEXT FROM reservation_cursor INTO @ReservationID;
    END
    
    CLOSE reservation_cursor;
    DEALLOCATE reservation_cursor;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 7. trg_Rezervasyon_Bildirim
-- =============================================
PRINT '7. trg_Rezervasyon_Bildirim oluşturuluyor...';

IF OBJECT_ID('app.trg_Rezervasyon_Bildirim', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Rezervasyon_Bildirim;
GO

CREATE TRIGGER app.trg_Rezervasyon_Bildirim
ON app.Reservations
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO log.NotificationQueue (
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
            ELSE 'Email'
        END,
        'Rezervasyonunuz Oluşturuldu',
        'Sayın ' + U.FullName + ', ' + 
        'Rezervasyonunuz başarıyla oluşturuldu. Rezervasyon ID: ' + CAST(I.ReservationID AS NVARCHAR(10)) + 
        '. Ödeme yapmak için rezervasyonlarınız sayfasını ziyaret edin.',
        I.ReservationID
    FROM inserted I
    INNER JOIN app.Users U ON I.UserID = U.UserID
    LEFT JOIN log.UserNotificationPreferences UNP ON I.UserID = UNP.UserID
    WHERE (UNP.ReservationNotifications = 1 OR UNP.ReservationNotifications IS NULL);
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 8. trg_Odeme_Bildirim
-- =============================================
PRINT '8. trg_Odeme_Bildirim oluşturuluyor...';

IF OBJECT_ID('app.trg_Odeme_Bildirim', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Odeme_Bildirim;
GO

CREATE TRIGGER app.trg_Odeme_Bildirim
ON app.Payments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO log.NotificationQueue (
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
    INNER JOIN app.Reservations R ON UPD.ReservationID = R.ReservationID
    INNER JOIN app.Users U ON R.UserID = U.UserID
    LEFT JOIN log.UserNotificationPreferences UNP ON R.UserID = UNP.UserID
    WHERE DEL.Status = 'Pending' 
      AND UPD.Status = 'Completed'
      AND (UNP.PaymentNotifications = 1 OR UNP.PaymentNotifications IS NULL);
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 9. trg_Iptal_Bildirim
-- =============================================
PRINT '9. trg_Iptal_Bildirim oluşturuluyor...';

IF OBJECT_ID('app.trg_Iptal_Bildirim', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Iptal_Bildirim;
GO

CREATE TRIGGER app.trg_Iptal_Bildirim
ON app.Reservations
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO log.NotificationQueue (
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
    INNER JOIN app.Users U ON UPD.UserID = U.UserID
    LEFT JOIN log.UserNotificationPreferences UNP ON UPD.UserID = UNP.UserID
    LEFT JOIN app.CancellationReasons CR ON UPD.CancelReasonID = CR.ReasonID
    WHERE DEL.Status != 'Cancelled' 
      AND UPD.Status = 'Cancelled'
      AND (UNP.CancellationNotifications = 1 OR UNP.CancellationNotifications IS NULL);
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 10. trg_Kullanici_Kayit_Bildirim
-- =============================================
PRINT '10. trg_Kullanici_Kayit_Bildirim oluşturuluyor...';

IF OBJECT_ID('app.trg_Kullanici_Kayit_Bildirim', 'TR') IS NOT NULL
    DROP TRIGGER app.trg_Kullanici_Kayit_Bildirim;
GO

CREATE TRIGGER app.trg_Kullanici_Kayit_Bildirim
ON app.Users
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO log.NotificationQueue (
        UserID,
        NotificationType,
        NotificationMethod,
        Subject,
        Message,
        RelatedReservationID
    )
    SELECT 
        I.UserID,
        'Registration',
        'Email',
        'RayBus''a Hoş Geldiniz! 🎉',
        'Sayın ' + I.FullName + ', ' + 
        'RayBus ailesine katıldığınız için teşekkür ederiz! ' +
        'Artık tren ve otobüs biletlerinizi kolayca rezerve edebilirsiniz. ' +
        'İyi yolculuklar dileriz! 🚌🚄',
        NULL
    FROM inserted I
    WHERE I.Status = 1;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- TAMAMLANDI
-- =============================================
PRINT '========================================';
PRINT 'TÜM TRIGGER''LAR BAŞARIYLA OLUŞTURULDU!';
PRINT '========================================';
PRINT '';
PRINT 'Oluşturulan Trigger''lar (app schema):';
PRINT '  1. trg_Rezervasyon_Sonrasi_Koltuk_Guncelle';
PRINT '  2. trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar';
PRINT '  3. trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur';
PRINT '  4. trg_Sefer_Guncellendiginde_Log_Tut';
PRINT '  5. trg_Odeme_Islemleri_Logla';
PRINT '  6. trg_Bilet_Numarasi';
PRINT '  7. trg_Rezervasyon_Bildirim';
PRINT '  8. trg_Odeme_Bildirim';
PRINT '  9. trg_Iptal_Bildirim';
PRINT '  10. trg_Kullanici_Kayit_Bildirim';
PRINT '';
PRINT 'NOT: Trigger''lar app schema''sında oluşturulur çünkü hedef tablolar app schema''sındadır.';
PRINT '';
GO

