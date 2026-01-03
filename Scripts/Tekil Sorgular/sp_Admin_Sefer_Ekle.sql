-- =============================================
-- Stored Procedure: sp_Admin_Sefer_Ekle
-- Açıklama: Admin panelinde sefer ekler (çakışma kontrolü ile)
-- Tüm alanları kaydeder: Terminal, Station, ArrivalDate, ArrivalTime
-- Parametreler:
--   @NeredenID: Kalkış şehir ID
--   @NereyeID: Varış şehir ID
--   @AracID: Araç ID
--   @Tarih: Kalkış tarihi
--   @Saat: Kalkış saati
--   @Fiyat: Sefer fiyatı
--   @KalkisTerminalID: Kalkış terminal ID (NULL olabilir)
--   @VarisTerminalID: Varış terminal ID (NULL olabilir)
--   @KalkisIstasyonID: Kalkış istasyon ID (NULL olabilir)
--   @VarisIstasyonID: Varış istasyon ID (NULL olabilir)
--   @VarisTarihi: Varış tarihi (NULL olabilir)
--   @VarisSaati: Varış saati (NULL olabilir)
-- =============================================

CREATE OR ALTER PROCEDURE sp_Admin_Sefer_Ekle
    @NeredenID INT,
    @NereyeID INT,
    @AracID INT,
    @Tarih DATE,
    @Saat TIME,
    @Fiyat DECIMAL(10,2),
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
        -- 1. KONTROL: Seçilen araç o gün başka bir seferde mi? (Çakışma Kontrolü)
        IF EXISTS (
            SELECT 1 FROM dbo.Trips 
            WHERE VehicleID = @AracID 
              AND DepartureDate = @Tarih 
              AND ABS(DATEDIFF(HOUR, DepartureTime, @Saat)) < 4 -- 4 saatlik çakışma payı
              AND Status = 1
        )
        BEGIN
            THROW 50001, 'Seçilen araç belirtilen saat aralığında başka bir seferde görünüyor.', 1;
        END

        -- 2. EKLEME İŞLEMİ (Tüm alanlar dahil)
        INSERT INTO dbo.Trips (
            VehicleID, 
            FromCityID, 
            ToCityID, 
            DepartureTerminalID,
            ArrivalTerminalID,
            DepartureStationID,
            ArrivalStationID,
            DepartureDate, 
            DepartureTime,
            ArrivalDate,
            ArrivalTime,
            Price, 
            Status, 
            CreatedAt
        )
        VALUES (
            @AracID, 
            @NeredenID, 
            @NereyeID,
            @KalkisTerminalID,
            @VarisTerminalID,
            @KalkisIstasyonID,
            @VarisIstasyonID,
            @Tarih, 
            @Saat,
            @VarisTarihi,
            @VarisSaati,
            @Fiyat, 
            1, 
            SYSUTCDATETIME()
        );

        -- 3. KOLTUKLARI OTOMATİK OLUŞTUR (Trigger ile de yapılabilir ama burada manuel yapıyoruz)
        DECLARE @YeniSeferID INT = SCOPE_IDENTITY();
        
        -- Araçtaki tüm koltukları sefer için oluştur
        INSERT INTO dbo.TripSeats (TripID, SeatID, IsReserved)
        SELECT @YeniSeferID, SeatID, 0
        FROM dbo.Seats
        WHERE VehicleID = @AracID;

        SELECT 'Sefer başarıyla planlandı. Koltuklar otomatik oluşturuldu.' AS Mesaj;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO
