-- =============================================
-- RayBus - Rezervasyon İptali Sonrası Koltuk Boşaltma Trigger'ı
-- =============================================
-- Rezervasyon iptal edildiğinde koltukları boşaltır
-- Kullanım: Veritabanında otomatik çalışır, rezervasyon iptal edildiğinde tetiklenir

USE [RayBus]
GO

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

PRINT 'trg_Rezervasyon_Iptali_Sonrasi_Koltuk_Bosa_Cikar trigger''ı başarıyla oluşturuldu!';
GO

