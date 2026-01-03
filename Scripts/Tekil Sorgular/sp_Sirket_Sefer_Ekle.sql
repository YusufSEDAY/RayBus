-- =============================================
-- Stored Procedure: sp_Sirket_Sefer_Ekle
-- Açıklama: Şirket panelinde sefer ekler (güvenlik ve çakışma kontrolü ile)
-- Parametreler:
--   @SirketID: Şirket ID (JWT token'dan alınır)
--   @NeredenID: Kalkış şehir ID
--   @NereyeID: Varış şehir ID
--   @AracID: Araç ID (şirkete ait olmalı)
--   @Tarih: Kalkış tarihi
--   @Saat: Kalkış saati
--   @Fiyat: Sefer fiyatı
-- =============================================

CREATE OR ALTER PROCEDURE sp_Sirket_Sefer_Ekle
    @SirketID INT,
    @NeredenID INT,
    @NereyeID INT,
    @AracID INT,
    @Tarih DATE,
    @Saat TIME,
    @Fiyat DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. GÜVENLİK: Araç senin mi?
        -- Eğer Admin eklediyse (NULL) veya başka şirketinse (ID farklı) HATA VERİR.
        IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE VehicleID = @AracID AND CompanyID = @SirketID)
        BEGIN
            THROW 50001, 'Yetkisiz işlem: Bu araç firmanıza ait değil.', 1;
        END

        -- 2. ÇAKIŞMA KONTROLÜ
        IF EXISTS (SELECT 1 FROM dbo.Trips WHERE VehicleID = @AracID AND DepartureDate = @Tarih AND ABS(DATEDIFF(HOUR, DepartureTime, @Saat)) < 4 AND Status = 1)
        BEGIN
            THROW 50002, 'Araç meşgul.', 1;
        END

        -- 3. SEFER EKLE
        INSERT INTO dbo.Trips (VehicleID, FromCityID, ToCityID, DepartureDate, DepartureTime, Price, Status, CreatedAt)
        VALUES (@AracID, @NeredenID, @NereyeID, @Tarih, @Saat, @Fiyat, 1, SYSUTCDATETIME());

        -- 4. LOGLAMA (TripLogs tablosunu da güncelleyelim)
        DECLARE @YeniSeferID INT = SCOPE_IDENTITY();
        INSERT INTO dbo.TripLogs (TripID, Action, NewValue, LogDate, Description)
        VALUES (@YeniSeferID, 'Create', CAST(@Fiyat AS NVARCHAR), SYSUTCDATETIME(), 'Oluşturan Şirket ID: ' + CAST(@SirketID AS NVARCHAR));

        SELECT 'Sefer başarıyla oluşturuldu.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

