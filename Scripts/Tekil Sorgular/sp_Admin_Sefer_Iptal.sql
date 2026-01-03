-- =============================================
-- Stored Procedure: sp_Admin_Sefer_Iptal
-- Açıklama: Admin panelinde sefer iptal eder (geçmiş sefer kontrolü ile)
-- Parametreler:
--   @SeferID: İptal edilecek sefer ID
--   @IptalNedeni: İptal nedeni (gelecekte log için)
-- =============================================

CREATE PROCEDURE sp_Admin_Sefer_Iptal
    @SeferID INT,
    @IptalNedeni NVARCHAR(200) -- Loglamak için (İlerde Log tablosuna eklenebilir)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. KONTROL: Geçmiş sefer iptal edilemez
        DECLARE @SeferTarihi DATETIME;
        SELECT @SeferTarihi = CAST(DepartureDate AS DATETIME) + CAST(DepartureTime AS DATETIME) 
        FROM dbo.Trips WHERE TripID = @SeferID;

        IF @SeferTarihi < GETDATE()
        BEGIN
            THROW 50001, 'Geçmiş tarihli bir sefer iptal edilemez.', 1;
        END

        -- 2. GÜNCELLEME: Durumu Pasif (0) veya İptal (2) yapalım.
        -- Bizim tasarımda Status: 1 (Aktif), 0 (Pasif/İptal) demiştik.
        UPDATE dbo.Trips
        SET Status = 0
        WHERE TripID = @SeferID;

        -- NOT: Bu işlem çalıştığında 'trg_Sefer_Guncellendiginde_Log_Tut' trigger'ı
        -- devreye girip "Status değişti" diye log tutacak.
        
        SELECT 'Sefer iptal edildi. İlgili loglar oluşturuldu.' AS Mesaj;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

