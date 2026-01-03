-- =============================================
-- sp_Rezervasyon_Yap - ZORLA GÜNCELLEME
-- =============================================
-- Bu script stored procedure'ü kesin olarak günceller
-- Eğer önceki script çalışmadıysa bu script'i kullanın

-- ÖNEMLİ: Connection string'inizdeki veritabanı adını kontrol edin!
-- Eğer veritabanı adı farklıysa, aşağıdaki USE komutunu güncelleyin

-- Veritabanını kontrol et
-- Connection string'deki veritabanı adı: RayBusDB
DECLARE @CurrentDB NVARCHAR(128) = DB_NAME()
PRINT 'Şu anki veritabanı: ' + @CurrentDB

IF @CurrentDB != 'RayBusDB' AND @CurrentDB != 'RayBus'
BEGIN
    PRINT '⚠️ UYARI: Veritabanı adı beklenen değil!'
    PRINT 'RayBusDB veya RayBus veritabanına geçin!'
    -- USE [RayBusDB] -- Gerekirse manuel olarak açın
END
ELSE
BEGIN
    PRINT '✅ Doğru veritabanında: ' + @CurrentDB
END
GO

PRINT '============================================='
PRINT 'sp_Rezervasyon_Yap Güncelleniyor...'
PRINT '============================================='
GO

-- Önce stored procedure'ün var olup olmadığını kontrol et
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Rezervasyon_Yap' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT 'Eski stored procedure bulundu, siliniyor...'
    DROP PROCEDURE [dbo].[sp_Rezervasyon_Yap]
    PRINT '✅ Eski stored procedure silindi'
END
ELSE
BEGIN
    PRINT '⚠️ Eski stored procedure bulunamadı (ilk kez oluşturuluyor)'
END
GO

-- Yeni stored procedure'ü oluştur
CREATE PROCEDURE [dbo].[sp_Rezervasyon_Yap]
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
        THROW;
    END CATCH
END;
GO

PRINT '============================================='
PRINT '✅ sp_Rezervasyon_Yap başarıyla oluşturuldu/güncellendi!'
PRINT '============================================='
GO

-- Kontrol: Parametreleri göster
PRINT ''
PRINT 'Parametreler:'
SELECT 
    p.parameter_id AS Sira,
    p.name AS ParametreAdi,
    t.name AS ParametreTipi
FROM sys.parameters p
INNER JOIN sys.types t ON p.user_type_id = t.user_type_id
WHERE p.object_id = OBJECT_ID('dbo.sp_Rezervasyon_Yap')
ORDER BY p.parameter_id;
GO

