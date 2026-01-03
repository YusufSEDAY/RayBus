-- =============================================
-- RayBus - Tüm Trigger'lar
-- =============================================
-- Bu script tüm trigger'ları oluşturur
-- Çalıştırmadan önce mevcut trigger'ları kontrol edin

USE [RayBus]
GO

-- =============================================
-- 1. trg_Rezervasyon_Sonrasi_Koltuk_Guncelle
-- =============================================
-- Rezervasyon yapıldığında koltuk durumunu günceller
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Rezervasyon_Sonrasi_Koltuk_Guncelle')
    DROP TRIGGER trg_Rezervasyon_Sonrasi_Koltuk_Guncelle;
GO

CREATE TRIGGER trg_Rezervasyon_Sonrasi_Koltuk_Guncelle
ON dbo.Reservations
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Yeni yapılan rezervasyondaki Sefer ve Koltuk bilgisini al,
        -- TripSeats tablosunda o koltuğu "Dolu" (1) olarak güncelle.
        UPDATE dbo.TripSeats
        SET IsReserved = 1,
            ReservedAt = SYSUTCDATETIME()
        FROM dbo.TripSeats TS
        INNER JOIN inserted i ON TS.TripID = i.TripID AND TS.SeatID = i.SeatID;
        
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- =============================================
-- 2. trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar
-- =============================================
-- Rezervasyon iptal edildiğinde koltukları boşaltır
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar')
    DROP TRIGGER trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar;
GO

CREATE TRIGGER trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar
ON dbo.Reservations
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Sadece durumu 'Cancelled' (İptal) olarak DEĞİŞEN kayıtları yakala
        IF UPDATE(Status)
        BEGIN
            -- İptal edilen rezervasyonların koltuklarını bul ve boşalt
            UPDATE dbo.TripSeats
            SET IsReserved = 0,
                ReservedAt = NULL
            FROM dbo.TripSeats TS
            INNER JOIN inserted i ON TS.TripID = i.TripID AND TS.SeatID = i.SeatID
            INNER JOIN deleted d ON i.ReservationID = d.ReservationID
            WHERE i.Status = 'Cancelled'      -- Yeni durum İptal ise
              AND d.Status <> 'Cancelled';    -- Eski durum İptal değilse (Gereksiz çalışmayı önler)
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

-- =============================================
-- 3. trg_Sefer_Guncellendiginde_Log_Tut
-- =============================================
-- Sefer güncellemelerinde audit log tutar
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Sefer_Guncellendiginde_Log_Tut')
    DROP TRIGGER trg_Sefer_Guncellendiginde_Log_Tut;
GO

CREATE TRIGGER trg_Sefer_Guncellendiginde_Log_Tut
ON dbo.Trips
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Fiyat Değişikliği Oldu mu?
    IF UPDATE(Price)
    BEGIN
        INSERT INTO dbo.TripLogs (TripID, ColumnName, OldValue, NewValue, ChangedAt)
        SELECT 
            i.TripID, 
            'Price', 
            CAST(d.Price AS NVARCHAR(50)), 
            CAST(i.Price AS NVARCHAR(50)), 
            SYSUTCDATETIME()
        FROM inserted i
        INNER JOIN deleted d ON i.TripID = d.TripID
        WHERE i.Price <> d.Price; -- Sadece gerçekten değiştiyse
    END

    -- 2. Durum (İptal/Aktif) Değişikliği Oldu mu?
    IF UPDATE(Status)
    BEGIN
        INSERT INTO dbo.TripLogs (TripID, ColumnName, OldValue, NewValue, ChangedAt)
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

    -- 3. Kalkış Tarihi Değişikliği Oldu mu?
    IF UPDATE(DepartureDate)
    BEGIN
        INSERT INTO dbo.TripLogs (TripID, ColumnName, OldValue, NewValue, ChangedAt)
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

PRINT 'Tüm trigger''lar başarıyla oluşturuldu!';
GO

