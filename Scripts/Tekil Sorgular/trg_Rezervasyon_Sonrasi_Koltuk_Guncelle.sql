-- =============================================
-- RayBus - Rezervasyon Sonrası Koltuk Güncelleme Trigger'ı
-- =============================================
-- Rezervasyon yapıldığında koltuk durumunu günceller
-- Kullanım: Veritabanında otomatik çalışır, rezervasyon yapıldığında tetiklenir

USE [RayBus]
GO

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

PRINT 'trg_Rezervasyon_Sonrasi_Koltuk_Guncelle trigger''ı başarıyla oluşturuldu!';
GO

