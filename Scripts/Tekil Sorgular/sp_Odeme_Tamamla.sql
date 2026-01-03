-- =============================================
-- sp_Odeme_Tamamla - Yeni Stored Procedure
-- =============================================
-- Senaryo B için: "Rezerve Et, Sonra Öderim" akışında
-- Kullanıcı önce rezervasyon yaptı (@IslemTipi = 0)
-- Sonra ödeme ekranına gidip ödeme yaptı
-- Bu SP ile ödeme tamamlanır

-- Connection string'deki veritabanı: RayBusDB
-- Eğer farklı bir veritabanı kullanıyorsanız, aşağıdaki satırı güncelleyin
USE [RayBusDB]  -- veya [RayBus] - hangisi doğruysa
GO

-- Eğer varsa eski versiyonu sil
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Odeme_Tamamla')
    DROP PROCEDURE sp_Odeme_Tamamla;
GO

CREATE PROCEDURE sp_Odeme_Tamamla
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
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

PRINT 'sp_Odeme_Tamamla stored procedure başarıyla oluşturuldu!';
GO

