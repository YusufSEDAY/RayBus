-- Trigger: trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur
-- Tetiklenme Zamanı: Trips tablosuna yeni bir sefer eklendiğinde (AFTER INSERT)
-- Amacı: Sefer planlandığı anda, o sefere ait satılabilir koltuk envanterini (TripSeats) otomatik olarak oluşturmak

IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur')
    DROP TRIGGER trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur;
GO

CREATE TRIGGER trg_Sefer_Eklendikten_Sonra_Koltuklari_Olustur
ON dbo.Trips
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Eklenen seferin ID'sini ve Araç bilgisini alıp,
        -- o aracın koltuklarını TripSeats tablosuna aktarıyoruz.
        INSERT INTO dbo.TripSeats (TripID, SeatID, IsReserved)
        SELECT 
            i.TripID, 
            s.SeatID, 
            0 -- Varsayılan: Boş (Rezerve Değil)
        FROM inserted i -- 'inserted': O an eklenen sefer satırı
        INNER JOIN dbo.Seats s ON s.VehicleID = i.VehicleID
        WHERE s.IsActive = 1; -- Sadece aktif koltuklar
    END TRY
    BEGIN CATCH
        -- Hata olursa işlemi geri al
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @HataMesaji NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@HataMesaji, 16, 1);
    END CATCH
END;
GO

