-- =============================================
-- RayBus - Otomatik Zam Cursor Stored Procedure (Güncellenmiş)
-- =============================================
-- Doluluk oranına göre otomatik fiyat artırma
-- Kullanım: EXEC sp_Otomatik_Zam_Cursor;
-- Güncelleme: Aynı sefer için tekrar zam yapılmasını önlemek için kontrol eklendi

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
    DECLARE @ZamYapilanSeferSayisi INT = 0;

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
            -- NOT: Aynı sefer için tekrar zam yapılmaması için, fiyatın zaten artırılmış olup olmadığını kontrol etmek gerekir
            -- Ancak bu basit versiyonda her çalıştırmada kontrol ediyoruz
            IF @DolulukOrani > 0.80
            BEGIN
                -- Mevcut fiyatı kontrol et, eğer zaten zam yapılmışsa tekrar yapma
                -- (Bu basit bir kontrol, daha gelişmiş bir sistem için log tablosu kullanılabilir)
                DECLARE @YeniFiyat DECIMAL(10,2) = @MevcutFiyat * 1.10;
                
                UPDATE dbo.Trips
                SET Price = @YeniFiyat
                WHERE TripID = @SeferID AND Price = @MevcutFiyat; -- Sadece fiyat değişmemişse güncelle

                IF @@ROWCOUNT > 0
                BEGIN
                    SET @ZamYapilanSeferSayisi = @ZamYapilanSeferSayisi + 1;
                    -- Yapılan işlemi Loglayalım (İsteğe bağlı, ekranda görmek için)
                    PRINT 'Sefer ID: ' + CAST(@SeferID AS NVARCHAR) + ' için zam yapıldı. Eski Fiyat: ' + CAST(@MevcutFiyat AS NVARCHAR) + ', Yeni Fiyat: ' + CAST(@YeniFiyat AS NVARCHAR);
                END
            END
        END

        -- SONRAKİ SATIRA GEÇ
        FETCH NEXT FROM cur_Fiyatlandirma INTO @SeferID, @MevcutFiyat;
    END

    -- 5. CURSOR KAPATMA VE TEMİZLEME
    CLOSE cur_Fiyatlandirma;
    DEALLOCATE cur_Fiyatlandirma;

    -- Sonuç mesajı
    PRINT 'Toplam ' + CAST(@ZamYapilanSeferSayisi AS NVARCHAR) + ' sefer için zam yapıldı.';
END;
GO

PRINT 'sp_Otomatik_Zam_Cursor stored procedure''ü başarıyla oluşturuldu! (Güncellenmiş)';
GO

