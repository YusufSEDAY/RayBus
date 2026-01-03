-- =============================================
-- Stored Procedure: sp_Admin_Sefer_Guncelle
-- Açıklama: Admin panelinde sefer bilgilerini günceller
-- Parametreler: Tüm alanlar opsiyonel (NULL ise güncellenmez)
-- =============================================

CREATE OR ALTER PROCEDURE sp_Admin_Sefer_Guncelle
    @SeferID INT,
    @NeredenID INT = NULL,
    @NereyeID INT = NULL,
    @AracID INT = NULL,
    @Tarih DATE = NULL,
    @Saat TIME = NULL,
    @Fiyat DECIMAL(10,2) = NULL,
    @KalkisTerminalID INT = NULL,
    @VarisTerminalID INT = NULL,
    @KalkisIstasyonID INT = NULL,
    @VarisIstasyonID INT = NULL,
    @VarisTarihi DATE = NULL,
    @VarisSaati TIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Sefer var mı kontrol et
        IF NOT EXISTS (SELECT 1 FROM dbo.Trips WHERE TripID = @SeferID)
        BEGIN
            THROW 50001, 'Sefer bulunamadı.', 1;
        END

        -- Geçmiş sefer kontrolü (sadece tarih değiştiriliyorsa)
        IF @Tarih IS NOT NULL OR @Saat IS NOT NULL
        BEGIN
            DECLARE @MevcutTarih DATE;
            DECLARE @MevcutSaat TIME;
            DECLARE @YeniTarih DATE = @Tarih;
            DECLARE @YeniSaat TIME = @Saat;

            SELECT @MevcutTarih = DepartureDate, @MevcutSaat = DepartureTime
            FROM dbo.Trips
            WHERE TripID = @SeferID;

            IF @YeniTarih IS NULL SET @YeniTarih = @MevcutTarih;
            IF @YeniSaat IS NULL SET @YeniSaat = @MevcutSaat;

            -- Geçmiş tarihli sefer kontrolü
            IF CAST(@YeniTarih AS DATETIME) + CAST(@YeniSaat AS DATETIME) < GETDATE()
            BEGIN
                THROW 50002, 'Geçmiş tarihli bir sefer güncellenemez.', 1;
            END
        END

        -- Çakışma kontrolü (araç veya tarih/saat değiştiriliyorsa)
        IF @AracID IS NOT NULL OR @Tarih IS NOT NULL OR @Saat IS NOT NULL
        BEGIN
            DECLARE @KontrolAracID INT = @AracID;
            DECLARE @KontrolTarih DATE = @Tarih;
            DECLARE @KontrolSaat TIME = @Saat;

            SELECT 
                @KontrolAracID = ISNULL(@KontrolAracID, VehicleID),
                @KontrolTarih = ISNULL(@KontrolTarih, DepartureDate),
                @KontrolSaat = ISNULL(@KontrolSaat, DepartureTime)
            FROM dbo.Trips
            WHERE TripID = @SeferID;

            IF EXISTS (
                SELECT 1 FROM dbo.Trips 
                WHERE VehicleID = @KontrolAracID 
                  AND DepartureDate = @KontrolTarih 
                  AND ABS(DATEDIFF(HOUR, DepartureTime, @KontrolSaat)) < 4
                  AND Status = 1
                  AND TripID != @SeferID
            )
            BEGIN
                THROW 50003, 'Seçilen araç belirtilen saat aralığında başka bir seferde görünüyor.', 1;
            END
        END

        -- Şehir kontrolü
        IF @NeredenID IS NOT NULL AND @NereyeID IS NOT NULL
        BEGIN
            IF @NeredenID = @NereyeID
            BEGIN
                THROW 50004, 'Kalkış ve varış şehirleri aynı olamaz.', 1;
            END
        END

        -- Güncelleme işlemi
        UPDATE dbo.Trips
        SET 
            FromCityID = ISNULL(@NeredenID, FromCityID),
            ToCityID = ISNULL(@NereyeID, ToCityID),
            VehicleID = ISNULL(@AracID, VehicleID),
            DepartureTerminalID = CASE 
                WHEN @KalkisTerminalID = -1 THEN NULL
                WHEN @KalkisTerminalID IS NOT NULL THEN @KalkisTerminalID
                ELSE DepartureTerminalID
            END,
            ArrivalTerminalID = CASE 
                WHEN @VarisTerminalID = -1 THEN NULL
                WHEN @VarisTerminalID IS NOT NULL THEN @VarisTerminalID
                ELSE ArrivalTerminalID
            END,
            DepartureStationID = CASE 
                WHEN @KalkisIstasyonID = -1 THEN NULL
                WHEN @KalkisIstasyonID IS NOT NULL THEN @KalkisIstasyonID
                ELSE DepartureStationID
            END,
            ArrivalStationID = CASE 
                WHEN @VarisIstasyonID = -1 THEN NULL
                WHEN @VarisIstasyonID IS NOT NULL THEN @VarisIstasyonID
                ELSE ArrivalStationID
            END,
            DepartureDate = ISNULL(@Tarih, DepartureDate),
            DepartureTime = ISNULL(@Saat, DepartureTime),
            ArrivalDate = CASE 
                WHEN @VarisTarihi = '1900-01-01' THEN NULL
                WHEN @VarisTarihi IS NOT NULL THEN @VarisTarihi
                ELSE ArrivalDate
            END,
            ArrivalTime = CASE 
                WHEN @VarisSaati = '00:00:00' THEN NULL
                WHEN @VarisSaati IS NOT NULL THEN @VarisSaati
                ELSE ArrivalTime
            END,
            Price = ISNULL(@Fiyat, Price)
        WHERE TripID = @SeferID;

        SELECT 'Sefer bilgileri başarıyla güncellendi.' AS Mesaj;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

