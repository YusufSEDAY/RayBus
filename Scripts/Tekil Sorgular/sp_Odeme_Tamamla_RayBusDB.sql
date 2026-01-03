-- =============================================
-- sp_Odeme_Tamamla - RayBusDB
-- =============================================
-- Connection string'deki veritabanı: RayBusDB
-- Bu script'i RayBusDB veritabanında çalıştırın

USE [RayBusDB]
GO

PRINT '============================================='
PRINT 'sp_Odeme_Tamamla Oluşturuluyor...'
PRINT 'Veritabanı: ' + DB_NAME()
PRINT '============================================='
GO

-- Eğer varsa eski versiyonu sil
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Odeme_Tamamla' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    DROP PROCEDURE [dbo].[sp_Odeme_Tamamla]
    PRINT '✅ Eski stored procedure silindi'
END
GO

CREATE PROCEDURE [dbo].[sp_Odeme_Tamamla]
    @RezervasyonID INT,
    @Fiyat DECIMAL(10,2),
    @OdemeYontemi NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    BEGIN TRY
        -- 1. KONTROL: Böyle bir rezervasyon var mı?
        IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE ReservationID = @RezervasyonID)
        BEGIN
            THROW 50001, 'Rezervasyon bulunamadı.', 1;
        END

        -- 2. KONTROL: Zaten ödenmiş mi veya İptal mi edilmiş?
        DECLARE @MevcutDurum NVARCHAR(30);
        DECLARE @OdemeDurumu NVARCHAR(30);

        SELECT @MevcutDurum = Status, @OdemeDurumu = PaymentStatus 
        FROM dbo.Reservations 
        WHERE ReservationID = @RezervasyonID;

        IF @MevcutDurum = 'Cancelled'
        BEGIN
            THROW 50002, 'İptal edilmiş bir rezervasyon için ödeme yapılamaz.', 1;
        END

        IF @OdemeDurumu = 'Paid'
        BEGIN
            THROW 50003, 'Bu rezervasyonun ödemesi zaten yapılmış.', 1;
        END

        -- 3. ADIM: Rezervasyonun ödeme durumunu güncelle
        UPDATE dbo.Reservations
        SET PaymentStatus = 'Paid'
        WHERE ReservationID = @RezervasyonID;

        -- 4. ADIM: Ödeme kaydını oluştur
        INSERT INTO dbo.Payments (ReservationID, Amount, PaymentMethod, Status)
        VALUES (@RezervasyonID, @Fiyat, @OdemeYontemi, 'Completed');

        -- İşlem Başarılı
        COMMIT TRANSACTION;

        SELECT 'Ödeme Başarıyla Tamamlandı' AS Mesaj;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        THROW;
    END CATCH
END;
GO

PRINT '✅ sp_Odeme_Tamamla stored procedure başarıyla oluşturuldu!'
GO

