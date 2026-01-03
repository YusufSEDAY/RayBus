-- =============================================
-- Stored Procedure: sp_Admin_Arac_Ekle
-- Açıklama: Admin panelinde veya şirket panelinde araç ekler ve otomatik koltukları oluşturur
-- Parametreler:
--   @PlakaNo: Araç plakası veya kodu (örn: '34 TB 1234')
--   @AracTipi: 'Bus' veya 'Train'
--   @ToplamKoltuk: Oluşturulacak koltuk sayısı
--   @SirketID: Şirket ID (opsiyonel, NULL ise admin ekliyor demektir)
-- =============================================

--sp_Admin_Arac_Ekle GÜNCELLEME(ŞİRKET İÇİN)
ALTER PROCEDURE sp_Admin_Arac_Ekle
    @PlakaNo NVARCHAR(50),
    @AracTipi NVARCHAR(20),
    @ToplamKoltuk INT, -- Bu parametreyi sadece döngü için kullanıyoruz
    @SirketID INT = NULL 
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. KONTROL: Mükerrer Plaka Var mı?
        IF EXISTS (SELECT 1 FROM dbo.Vehicles WHERE PlateOrCode = @PlakaNo)
        BEGIN
            THROW 50001, 'Bu plaka zaten kayıtlı.', 1;
        END

        -- 2. ARAÇ EKLEME
        -- DİKKAT: TotalSeats ve Status kolonlarını ÇIKARDIM.
        -- Sadece var olan kolonlara ekleme yapıyoruz.
        INSERT INTO dbo.Vehicles (VehicleType, PlateOrCode, CompanyID)
        VALUES (@AracTipi, @PlakaNo, @SirketID);

        DECLARE @YeniAracID INT = SCOPE_IDENTITY();

        -- 3. KOLTUKLARI OTOMATİK OLUŞTUR
        -- Tabloda TotalSeats tutmuyoruz ama koltukları oluşturarak dolaylı yoldan kapasiteyi belirliyoruz.
        DECLARE @Sayac INT = 1;
        WHILE @Sayac <= @ToplamKoltuk
        BEGIN
            INSERT INTO dbo.Seats (VehicleID, SeatNo, SeatPosition, WagonID)
            VALUES (
                @YeniAracID, 
                CAST(@Sayac AS NVARCHAR), 
                CASE WHEN @Sayac % 2 = 0 THEN 'Koridor' ELSE 'Cam Kenarı' END, 
                NULL
            );
            SET @Sayac = @Sayac + 1;
        END

        SELECT 'Araç başarıyla eklendi.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

