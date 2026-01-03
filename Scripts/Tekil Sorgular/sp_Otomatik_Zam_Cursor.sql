-- =============================================
-- RayBus - Otomatik Zam Cursor Stored Procedure
-- =============================================
-- Doluluk oranına göre otomatik fiyat artırma
-- Kullanım: EXEC sp_Otomatik_Zam_Cursor;

USE [RayBus]
GO

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Otomatik_Zam_Cursor')
    DROP PROCEDURE sp_Otomatik_Zam_Cursor;
GO

CREATE PROCEDURE sp_Otomatik_Zam_Cursor
AS
BEGIN
    SET NOCOUNT ON;

    -- Değişkenler
    DECLARE @SeferID INT;
    DECLARE @MevcutFiyat DECIMAL(10,2);
    DECLARE @ToplamKoltuk INT;
    DECLARE @DoluKoltuk INT;
    DECLARE @DolulukOrani FLOAT;

    -- 1. CURSOR TANIMLAMA
    -- Sadece gelecekteki aktif seferleri getiren bir imleç tanımlıyoruz
    DECLARE cur_Fiyatlandirma CURSOR FOR
    SELECT TripID, Price 
    FROM dbo.Trips 
    WHERE DepartureDate >= CAST(GETDATE() AS DATE) AND Status = 1;

    -- 2. CURSOR AÇMA
    OPEN cur_Fiyatlandirma;

    -- 3. İLK SATIRI OKUMA
    FETCH NEXT FROM cur_Fiyatlandirma INTO @SeferID, @MevcutFiyat;

    -- 4. DÖNGÜ (Satırlar bitene kadar dön)
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Her sefer için doluluk oranını hesapla
        SELECT @ToplamKoltuk = COUNT(*) FROM dbo.TripSeats WHERE TripID = @SeferID;
        SELECT @DoluKoltuk = COUNT(*) FROM dbo.TripSeats WHERE TripID = @SeferID AND IsReserved = 1;

        -- Bölme işleminde sıfıra bölünme hatasını önle
        IF @ToplamKoltuk > 0
        BEGIN
            SET @DolulukOrani = CAST(@DoluKoltuk AS FLOAT) / CAST(@ToplamKoltuk AS FLOAT);

            -- EĞER Doluluk %80'in üzerindeyse Fiyata %10 Zam Yap
            IF @DolulukOrani > 0.80
            BEGIN
                UPDATE dbo.Trips
                SET Price = @MevcutFiyat * 1.10 -- %10 Artır
                WHERE TripID = @SeferID;

                -- Yapılan işlemi Loglayalım (İsteğe bağlı, ekranda görmek için)
                PRINT 'Sefer ID: ' + CAST(@SeferID AS NVARCHAR) + ' için zam yapıldı. Yeni Fiyat: ' + CAST(@MevcutFiyat * 1.10 AS NVARCHAR);
            END
        END

        -- SONRAKİ SATIRA GEÇ
        FETCH NEXT FROM cur_Fiyatlandirma INTO @SeferID, @MevcutFiyat;
    END

    -- 5. CURSOR KAPATMA VE TEMİZLEME
    CLOSE cur_Fiyatlandirma;
    DEALLOCATE cur_Fiyatlandirma;
END;
GO

PRINT 'sp_Otomatik_Zam_Cursor stored procedure''ü başarıyla oluşturuldu!';
GO

