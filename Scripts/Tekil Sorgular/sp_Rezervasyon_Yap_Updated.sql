-- =============================================
-- sp_Rezervasyon_Yap - Güncellenmiş Versiyon
-- =============================================
-- YENİ ÖZELLİK: @IslemTipi parametresi eklendi
-- 0 = Sadece Rezervasyon (PaymentStatus: Pending)
-- 1 = Satın Alma (PaymentStatus: Paid, Payment kaydı oluşturulur)

-- Connection string'deki veritabanı: RayBusDB
-- Eğer farklı bir veritabanı kullanıyorsanız, aşağıdaki satırı güncelleyin
USE [RayBusDB]  -- veya [RayBus] - hangisi doğruysa
GO

-- Eski versiyonu sil
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Rezervasyon_Yap')
    DROP PROCEDURE sp_Rezervasyon_Yap;
GO

-- Güncellenmiş versiyonu oluştur
CREATE PROCEDURE sp_Rezervasyon_Yap
    @SeferID INT,
    @KoltukID INT,
    @KullaniciID INT,
    @Fiyat DECIMAL(10,2),
    @OdemeYontemi NVARCHAR(50),
    @IslemTipi TINYINT -- YENİ PARAMETRE: 0 = Sadece Rezervasyon, 1 = Satın Alma
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;

    BEGIN TRY
        -- 1. KONTROL: Koltuk hala boş mu?
        IF EXISTS (SELECT 1 FROM dbo.TripSeats WHERE TripID = @SeferID AND SeatID = @KoltukID AND IsReserved = 1)
        BEGIN
            THROW 50001, 'Üzgünüz, seçilen koltuk az önce başkası tarafından satın alındı.', 1;
        END

        -- 2. ADIM: Rezervasyon Kaydı Oluştur
        DECLARE @YeniRezervasyonID INT;
        DECLARE @OdemeDurumu NVARCHAR(30);

        -- İşlem tipine göre ödeme durumunu belirle
        IF @IslemTipi = 1 
            SET @OdemeDurumu = 'Paid';      -- Satın Almada: Ödendi
        ELSE 
            SET @OdemeDurumu = 'Pending';   -- Rezervasyonda: Bekliyor
        
        -- Rezervasyonu ekle
        INSERT INTO dbo.Reservations (TripID, SeatID, UserID, Status, PaymentStatus)
        VALUES (@SeferID, @KoltukID, @KullaniciID, 'Reserved', @OdemeDurumu); 
        
        SET @YeniRezervasyonID = SCOPE_IDENTITY();

        -- 3. ADIM: Ödeme Kaydı (SADECE SATIN ALMA İSE YAPILACAK)
        IF @IslemTipi = 1
        BEGIN
            INSERT INTO dbo.Payments (ReservationID, Amount, PaymentMethod, Status)
            VALUES (@YeniRezervasyonID, @Fiyat, @OdemeYontemi, 'Completed');
        END

        -- Koltuk durumu Trigger ile otomatik güncellenir (IsReserved = 1 olur)
        
        COMMIT TRANSACTION;
        
        -- Frontend'e bilgi dön
        SELECT 
            'Başarılı' AS Sonuc, 
            @YeniRezervasyonID AS RezervasyonID,
            @OdemeDurumu AS OdemeDurumu; -- Frontend anlasın diye durumu da dönüyoruz

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

PRINT 'sp_Rezervasyon_Yap stored procedure başarıyla güncellendi!';
PRINT 'Yeni parametre: @IslemTipi (0 = Rezervasyon, 1 = Satın Alma)';
GO

