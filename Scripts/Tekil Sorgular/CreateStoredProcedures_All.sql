-- =============================================
-- RayBus - Tüm Stored Procedure'ler
-- =============================================
-- Bu script tüm stored procedure'leri oluşturur

USE [RayBus]
GO

-- =============================================
-- 1. sp_Seferleri_Listele
-- =============================================
-- Güzergah ve tarih bazlı sefer arama
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Seferleri_Listele')
    DROP PROCEDURE sp_Seferleri_Listele;
GO

CREATE PROCEDURE sp_Seferleri_Listele
    @NeredenID INT,
    @NereyeID INT,
    @Tarih DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        T.TripID,
        F.CityName AS KalkisSehri,
        TC.CityName AS VarisSehri,
        T.DepartureDate,
        CONVERT(VARCHAR(5), T.DepartureTime, 108) AS KalkisSaati,
        T.Price,
        V.VehicleType, 
        V.PlateOrCode AS AracPlakaNo,
        
        -- Model bilgisi (Otobüs veya Tren)
        COALESCE(B.BusModel, TR.TrainModel, '') AS AracModeli,
        
        -- Otobüs için Layout Type
        B.LayoutType AS KoltukDuzeni,
        
        -- Boş koltuk sayısı
        (SELECT COUNT(*) 
         FROM dbo.TripSeats TS 
         WHERE TS.TripID = T.TripID AND TS.IsReserved = 0) AS BosKoltukSayisi,

        -- Kalkış Noktası (Terminal veya İstasyon)
        COALESCE(DT.TerminalName, DS.StationName) AS KalkisNoktasi,
        
        -- Varış Noktası
        COALESCE(ArrTerm.TerminalName, ArrS.StationName) AS VarisNoktasi

    FROM dbo.Trips T
    INNER JOIN dbo.Cities F ON T.FromCityID = F.CityID
    INNER JOIN dbo.Cities TC ON T.ToCityID = TC.CityID
    INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
    
    -- Otobüs bilgisi
    LEFT JOIN dbo.Buses B ON V.VehicleID = B.BusID AND V.VehicleType = 'Bus'
    
    -- Tren bilgisi
    LEFT JOIN dbo.Trains TR ON V.VehicleID = TR.TrainID AND V.VehicleType = 'Train'
    
    -- Terminaller 
    LEFT JOIN dbo.Terminals DT ON T.DepartureTerminalID = DT.TerminalID
    LEFT JOIN dbo.Terminals ArrTerm ON T.ArrivalTerminalID = ArrTerm.TerminalID
    
    -- İstasyonlar
    LEFT JOIN dbo.Stations DS ON T.DepartureStationID = DS.StationID
    LEFT JOIN dbo.Stations ArrS ON T.ArrivalStationID = ArrS.StationID

    WHERE 
        T.FromCityID = @NeredenID 
        AND T.ToCityID = @NereyeID
        AND T.DepartureDate = @Tarih
        AND T.Status = 1
    ORDER BY T.DepartureTime ASC;
END;
GO

-- =============================================
-- 2. sp_Sefer_Koltuk_Durumu
-- =============================================
-- Sefer koltuk durumunu getirir
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Sefer_Koltuk_Durumu')
    DROP PROCEDURE sp_Sefer_Koltuk_Durumu;
GO

CREATE PROCEDURE sp_Sefer_Koltuk_Durumu
    @SeferID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        TS.SeatID,
        S.SeatNo,          -- Koltuk Numarası (1A, 2B vb.)
        S.SeatPosition,    -- Cam Kenarı / Koridor
        TS.IsReserved,     -- 1: Dolu, 0: Boş
        
        -- Eğer Tren ise Vagon numarasını da getir, Otobüs ise NULL gelir
        W.WagonNo AS VagonNo

    FROM dbo.TripSeats TS
    INNER JOIN dbo.Seats S ON TS.SeatID = S.SeatID
    LEFT JOIN dbo.Wagons W ON S.WagonID = W.WagonID
    WHERE TS.TripID = @SeferID
    
    -- Listeyi önce Vagon numarasına (Trense), sonra Koltuk Numarasına göre sırala
    ORDER BY W.WagonNo, LEN(S.SeatNo), S.SeatNo;
END;
GO

-- =============================================
-- 3. sp_Rezervasyon_Yap
-- =============================================
-- Rezervasyon ve ödeme kaydı oluşturur (Transaction ile)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Rezervasyon_Yap')
    DROP PROCEDURE sp_Rezervasyon_Yap;
GO

CREATE PROCEDURE sp_Rezervasyon_Yap
    @SeferID INT,
    @KoltukID INT,
    @KullaniciID INT,
    @Fiyat DECIMAL(10,2),
    @OdemeYontemi NVARCHAR(50) -- Örn: 'Kredi Kartı', 'Havale'
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Transaction Başlat: Ya hepsi yapılır ya hiçbiri yapılmaz (Atomicity)
    BEGIN TRANSACTION;

    BEGIN TRY
        -- 1. KONTROL: Koltuk hala boş mu? (Race Condition Kontrolü)
        -- Müşteri butona basana kadar başkası kapmış olabilir.
        IF EXISTS (SELECT 1 FROM dbo.TripSeats WHERE TripID = @SeferID AND SeatID = @KoltukID AND IsReserved = 1)
        BEGIN
            -- Hata fırlat ve işlemi durdur
            THROW 50001, 'Üzgünüz, seçilen koltuk az önce başkası tarafından satın alındı.', 1;
        END

        -- 2. ADIM: Rezervasyon Kaydı Oluştur
        DECLARE @YeniRezervasyonID INT;
        
        INSERT INTO dbo.Reservations (TripID, SeatID, UserID, Status, PaymentStatus)
        VALUES (@SeferID, @KoltukID, @KullaniciID, 'Reserved', 'Paid'); 
        
        SET @YeniRezervasyonID = SCOPE_IDENTITY(); -- Oluşan ID'yi al

        -- 3. ADIM: Ödeme Kaydı Oluştur
        INSERT INTO dbo.Payments (ReservationID, Amount, PaymentMethod, Status)
        VALUES (@YeniRezervasyonID, @Fiyat, @OdemeYontemi, 'Completed');

        -- NOT: Koltuğu "Dolu" (IsReserved=1) yapma işini, daha önce yazdığımız TRIGGER otomatik yapacak.
        -- O yüzden burada update yazmıyoruz.

        -- Her şey yolundaysa işlemi onayla
        COMMIT TRANSACTION;
        
        -- Frontend'e başarılı bilgisini dön
        SELECT 'Başarılı' AS Sonuc, @YeniRezervasyonID AS RezervasyonID;

    END TRY
    BEGIN CATCH
        -- Hata olursa (örn: para çekilemedi, veritabanı hatası vs.)
        -- Yapılan tüm işlemleri geri al (Rollback)
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

-- =============================================
-- 4. sp_Kullanici_Biletleri
-- =============================================
-- Kullanıcının tüm biletlerini getirir
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_Kullanici_Biletleri')
    DROP PROCEDURE sp_Kullanici_Biletleri;
GO

CREATE PROCEDURE sp_Kullanici_Biletleri
    @KullaniciID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        R.ReservationID,
        T.TripID, -- Sefer ID'si eklendi (Detayları Gör butonu için gerekli)
        
        -- Güzergah Bilgisi
        F.CityName + ' > ' + T_City.CityName AS Guzergah,
        
        -- Tarih ve Saat
        T.DepartureDate,
        CONVERT(VARCHAR(5), T.DepartureTime, 108) AS KalkisSaati,
        
        -- Araç ve Koltuk
        V.VehicleType, -- Otobüs/Tren
        V.PlateOrCode,
        S.SeatNo,
        
        -- Finansal ve Durum
        P.Amount AS OdenenTutar,
        R.Status AS RezervasyonDurumu, -- 'Reserved', 'Cancelled'
        R.PaymentStatus,
        R.ReservationDate AS IslemTarihi

    FROM dbo.Reservations R
    INNER JOIN dbo.Trips T ON R.TripID = T.TripID
    INNER JOIN dbo.Cities F ON T.FromCityID = F.CityID      -- Nereden
    INNER JOIN dbo.Cities T_City ON T.ToCityID = T_City.CityID -- Nereye
    INNER JOIN dbo.Seats S ON R.SeatID = S.SeatID
    INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID
    LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID

    WHERE R.UserID = @KullaniciID
    ORDER BY T.DepartureDate DESC, T.DepartureTime DESC; -- En yeni sefer en üstte
END;
GO

-- =============================================
-- 5. sp_Otomatik_Zam_Cursor
-- =============================================
-- Doluluk oranına göre otomatik fiyat artırma
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

PRINT 'Tüm stored procedure''ler başarıyla oluşturuldu!';
GO

