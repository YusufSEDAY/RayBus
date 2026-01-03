-- =============================================
-- RayBus - Şirket Sefer Güncelleme Stored Procedure
-- =============================================
-- Şirket kendi seferlerini güncelleyebilir (güvenlik kontrolü ile)
-- Kullanım: EXEC sp_Sirket_Sefer_Guncelle @SirketID = 1, @SeferID = 10, @YeniFiyat = 150.00, @YeniTarih = '2024-12-25', @YeniSaat = '10:00';

USE [RayBus]
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Sirket_Sefer_Guncelle')
    DROP PROCEDURE sp_Sirket_Sefer_Guncelle;
GO

CREATE OR ALTER PROCEDURE sp_Sirket_Sefer_Guncelle
    @SirketID INT,
    @SeferID INT,
    @YeniFiyat DECIMAL(10,2),
    @YeniTarih DATE,
    @YeniSaat TIME
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- GÜVENLİK: Seferin Aracı Senin mi?
        IF NOT EXISTS (
            SELECT 1 FROM dbo.Trips T
            INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
            WHERE T.TripID = @SeferID AND V.CompanyID = @SirketID
        )
        BEGIN
            THROW 50001, 'Bu sefere müdahale yetkiniz yok.', 1;
        END

        UPDATE dbo.Trips
        SET Price = @YeniFiyat, DepartureDate = @YeniTarih, DepartureTime = @YeniSaat
        WHERE TripID = @SeferID;

        -- Loglama Trigger tarafından yapılacak ama biz manuel de ekleyebiliriz (Gerek yok, trigger yeterli)
        SELECT 'Sefer güncellendi.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

PRINT 'sp_Sirket_Sefer_Guncelle stored procedure''ü başarıyla oluşturuldu!';
GO

