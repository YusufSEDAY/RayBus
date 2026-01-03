-- =============================================
-- Stored Procedure: sp_Admin_Arac_Guncelle
-- Açıklama: Admin panelinde araç bilgilerini günceller
-- Parametreler:
--   @AracID: Güncellenecek araç ID
--   @PlakaNo: Yeni plaka/kod (NULL ise güncellenmez)
--   @AracTipi: Yeni araç tipi (NULL ise güncellenmez)
--   @Aktif: Yeni aktif durumu (NULL ise güncellenmez)
--   @SirketID: Yeni şirket ID (NULL ise güncellenmez, -1 ise NULL yapılır)
-- =============================================

CREATE OR ALTER PROCEDURE sp_Admin_Arac_Guncelle
    @AracID INT,
    @PlakaNo NVARCHAR(50) = NULL,
    @AracTipi NVARCHAR(20) = NULL,
    @Aktif BIT = NULL,
    @SirketID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Araç var mı kontrol et
        IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE VehicleID = @AracID)
        BEGIN
            THROW 50001, 'Araç bulunamadı.', 1;
        END

        -- Plaka kontrolü (eğer değiştiriliyorsa)
        IF @PlakaNo IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM dbo.Vehicles WHERE PlateOrCode = @PlakaNo AND VehicleID != @AracID)
            BEGIN
                THROW 50002, 'Bu plaka/kod başka bir araç tarafından kullanılıyor.', 1;
            END
        END

        -- Şirket kontrolü (eğer değiştiriliyorsa)
        IF @SirketID IS NOT NULL AND @SirketID != -1
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserID = @SirketID AND RoleID = 3) -- 3 = Şirket rolü
            BEGIN
                THROW 50003, 'Geçersiz şirket ID.', 1;
            END
        END

        -- Güncelleme işlemi
        UPDATE dbo.Vehicles
        SET 
            PlateOrCode = ISNULL(@PlakaNo, PlateOrCode),
            VehicleType = ISNULL(@AracTipi, VehicleType),
            Active = ISNULL(@Aktif, Active),
            CompanyID = CASE 
                WHEN @SirketID = -1 THEN NULL
                WHEN @SirketID IS NOT NULL THEN @SirketID
                ELSE CompanyID
            END
        WHERE VehicleID = @AracID;

        SELECT 'Araç bilgileri başarıyla güncellendi.' AS Mesaj;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

