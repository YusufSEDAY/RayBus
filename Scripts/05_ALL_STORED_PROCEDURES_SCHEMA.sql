-- =============================================
-- TÜM STORED PROCEDURE'LER - SCHEMA ORGANİZE EDİLMİŞ VERSİYON
-- =============================================
-- Bu dosya tüm stored procedure'leri proc schema'sında oluşturur
-- İçeriklerinde app ve log schema referansları kullanılır
-- Tarih: 2024-12-19
-- =============================================

USE [RayBusDB]
GO

PRINT '========================================';
PRINT 'TÜM STORED PROCEDURE''LER OLUŞTURULUYOR...';
PRINT '========================================';
PRINT '';

-- =============================================
-- 1. sp_Kullanici_Kayit
-- =============================================
PRINT '1. sp_Kullanici_Kayit oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Kullanici_Kayit', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Kullanici_Kayit;
GO

CREATE PROCEDURE [proc].sp_Kullanici_Kayit
    @AdSoyad NVARCHAR(100),
    @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(300), -- BCrypt ile hash'lenmiş şifre (backend'den gelir)
    @Telefon NVARCHAR(15),
    @RolAdi NVARCHAR(50) -- 'Müşteri' veya 'Şirket'
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. KONTROL: Bu e-posta zaten var mı?
        IF EXISTS (SELECT 1 FROM app.Users WHERE Email = @Email)
        BEGIN
            THROW 50001, 'Bu e-posta adresi ile daha önce kayıt olunmuş.', 1;
        END

        -- 2. GÜVENLİK KONTROLÜ: Biri 'Admin' olmaya mı çalışıyor?
        IF @RolAdi = 'Admin'
        BEGIN
            THROW 50002, 'Güvenlik ihlali: Admin rolü ile dışarıdan kayıt olunamaz.', 1;
        END

        -- 3. ROL ID BULMA
        DECLARE @SecilenRoleID INT;
        SELECT @SecilenRoleID = RoleID FROM app.Roles WHERE RoleName = @RolAdi;

        IF @SecilenRoleID IS NULL
        BEGIN
            THROW 50003, 'Geçersiz rol seçimi. Lütfen Müşteri veya Şirket seçiniz.', 1;
        END

        -- 4. KAYIT İŞLEMİ
        INSERT INTO app.Users (RoleID, FullName, Email, PasswordHash, Phone, Status, CreatedAt)
        VALUES (
            @SecilenRoleID, 
            @AdSoyad, 
            @Email, 
            @PasswordHash,
            @Telefon, 
            1,
            SYSUTCDATETIME()
        );

        SELECT 'Kayıt Başarılı' AS Mesaj, CAST(SCOPE_IDENTITY() AS INT) AS YeniUserID;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 2. sp_Kullanici_Giris
-- =============================================
PRINT '2. sp_Kullanici_Giris oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Kullanici_Giris', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Kullanici_Giris;
GO

CREATE PROCEDURE [proc].sp_Kullanici_Giris
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID INT;
    DECLARE @AdSoyad NVARCHAR(100);
    DECLARE @RoleID INT;
    DECLARE @RoleName NVARCHAR(50);
    DECLARE @PasswordHash NVARCHAR(300);
    DECLARE @Telefon NVARCHAR(15);
    DECLARE @Durum TINYINT;
    DECLARE @CreatedAt DATETIME2;

    -- 1. Kullanıcıyı Email ile bul
    SELECT 
        @UserID = U.UserID,
        @AdSoyad = U.FullName,
        @RoleID = U.RoleID,
        @RoleName = R.RoleName,
        @PasswordHash = U.PasswordHash,
        @Telefon = U.Phone,
        @Durum = U.Status,
        @CreatedAt = U.CreatedAt
    FROM app.Users U
    INNER JOIN app.Roles R ON U.RoleID = R.RoleID
    WHERE U.Email = @Email;

    -- 2. Kullanıcı yoksa
    IF @UserID IS NULL
    BEGIN
        SELECT 
            CAST(0 AS BIT) AS Basarili, 
            'E-posta veya şifre hatalı.' AS Mesaj,
            NULL AS UserID,
            NULL AS AdSoyad,
            NULL AS RoleID,
            NULL AS RolAdi,
            NULL AS PasswordHash,
            NULL AS Telefon,
            NULL AS CreatedAt;
        RETURN;
    END

    -- 3. Kullanıcı Pasif ise
    IF @Durum = 0
    BEGIN
        SELECT 
            CAST(0 AS BIT) AS Basarili, 
            'Hesabınız pasif durumdadır. Yönetici ile görüşün.' AS Mesaj,
            NULL AS UserID,
            NULL AS AdSoyad,
            NULL AS RoleID,
            NULL AS RolAdi,
            NULL AS PasswordHash,
            NULL AS Telefon,
            NULL AS CreatedAt;
        RETURN;
    END

    -- 4. Kullanıcı bulundu
    SELECT 
        CAST(1 AS BIT) AS Basarili, 
        'Kullanıcı bulundu' AS Mesaj,
        @UserID AS UserID,
        @AdSoyad AS AdSoyad,
        @RoleID AS RoleID,
        @RoleName AS RolAdi,
        @PasswordHash AS PasswordHash,
        @Telefon AS Telefon,
        @CreatedAt AS CreatedAt;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 3. sp_Rezervasyon_Yap
-- =============================================
PRINT '3. sp_Rezervasyon_Yap oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Rezervasyon_Yap', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Rezervasyon_Yap;
GO

CREATE PROCEDURE [proc].sp_Rezervasyon_Yap
    @SeferID INT,
    @KoltukID INT,
    @KullaniciID INT,
    @Fiyat DECIMAL(10,2),
    @OdemeYontemi NVARCHAR(50),
    @IslemTipi TINYINT -- 0 = Sadece Rezervasyon, 1 = Satın Alma
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRANSACTION;

    BEGIN TRY
        -- 1. KONTROL: Koltuk hala boş mu?
        IF EXISTS (SELECT 1 FROM app.TripSeats WHERE TripID = @SeferID AND SeatID = @KoltukID AND IsReserved = 1)
        BEGIN
            THROW 50001, 'Üzgünüz, seçilen koltuk az önce başkası tarafından satın alındı.', 1;
        END

        -- 2. ADIM: Rezervasyon Kaydı Oluştur
        DECLARE @YeniRezervasyonID INT;
        DECLARE @OdemeDurumu NVARCHAR(30);

        IF @IslemTipi = 1 
            SET @OdemeDurumu = 'Paid';
        ELSE 
            SET @OdemeDurumu = 'Pending';
        
        INSERT INTO app.Reservations (TripID, SeatID, UserID, Status, PaymentStatus, ReservationDate)
        VALUES (@SeferID, @KoltukID, @KullaniciID, 'Reserved', @OdemeDurumu, GETDATE()); 
        
        SET @YeniRezervasyonID = SCOPE_IDENTITY();

        -- 3. ADIM: Ödeme Kaydı (SADECE SATIN ALMA İSE)
        IF @IslemTipi = 1
        BEGIN
            INSERT INTO app.Payments (ReservationID, Amount, PaymentMethod, Status, PaymentDate)
            VALUES (@YeniRezervasyonID, @Fiyat, @OdemeYontemi, 'Completed', GETDATE());
        END
        
        COMMIT TRANSACTION;
        
        SELECT 
            'Başarılı' AS Sonuc, 
            @YeniRezervasyonID AS RezervasyonID,
            @OdemeDurumu AS OdemeDurumu;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 4. sp_Odeme_Tamamla
-- =============================================
PRINT '4. sp_Odeme_Tamamla oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Odeme_Tamamla', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Odeme_Tamamla;
GO

CREATE PROCEDURE [proc].sp_Odeme_Tamamla
    @RezervasyonID INT,
    @Fiyat DECIMAL(10,2),
    @OdemeYontemi NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    BEGIN TRY
        -- 1. KONTROL: Böyle bir rezervasyon var mı?
        IF NOT EXISTS (SELECT 1 FROM app.Reservations WHERE ReservationID = @RezervasyonID)
        BEGIN
            THROW 50001, 'Rezervasyon bulunamadı.', 1;
        END

        -- 2. KONTROL: Zaten ödenmiş mi veya İptal mi edilmiş?
        DECLARE @MevcutDurum NVARCHAR(30);
        DECLARE @OdemeDurumu NVARCHAR(30);

        SELECT @MevcutDurum = Status, @OdemeDurumu = PaymentStatus 
        FROM app.Reservations 
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
        UPDATE app.Reservations
        SET PaymentStatus = 'Paid'
        WHERE ReservationID = @RezervasyonID;

        -- 4. ADIM: Ödeme kaydını oluştur
        INSERT INTO app.Payments (ReservationID, Amount, PaymentMethod, Status, PaymentDate)
        VALUES (@RezervasyonID, @Fiyat, @OdemeYontemi, 'Completed', GETDATE());

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

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 5. sp_Kullanici_Biletleri
-- =============================================
PRINT '5. sp_Kullanici_Biletleri oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Kullanici_Biletleri', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Kullanici_Biletleri;
GO

CREATE PROCEDURE [proc].sp_Kullanici_Biletleri
    @KullaniciID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        R.ReservationID,
        T.TripID,
        
        -- Güzergah Bilgisi
        F.CityName + ' > ' + T_City.CityName AS Guzergah,
        
        -- Tarih ve Saat
        T.DepartureDate,
        CONVERT(VARCHAR(5), T.DepartureTime, 108) AS KalkisSaati,
        
        -- Araç ve Koltuk
        V.VehicleType,
        V.PlateOrCode,
        S.SeatNo,
        
        -- Finansal ve Durum
        P.Amount AS OdenenTutar,
        T.Price AS TripFiyati,
        R.Status AS RezervasyonDurumu,
        R.PaymentStatus,
        R.ReservationDate AS IslemTarihi

    FROM app.Reservations R
    INNER JOIN app.Trips T ON R.TripID = T.TripID
    INNER JOIN app.Cities F ON T.FromCityID = F.CityID
    INNER JOIN app.Cities T_City ON T.ToCityID = T_City.CityID
    INNER JOIN app.Seats S ON R.SeatID = S.SeatID
    INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
    LEFT JOIN app.Payments P ON R.ReservationID = P.ReservationID AND P.Status = 'Completed'

    WHERE R.UserID = @KullaniciID
    ORDER BY T.DepartureDate DESC, T.DepartureTime DESC;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 6. sp_Kullanici_Istatistikleri_Getir
-- =============================================
PRINT '6. sp_Kullanici_Istatistikleri_Getir oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Kullanici_Istatistikleri_Getir', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Kullanici_Istatistikleri_Getir;
GO

CREATE PROCEDURE [proc].sp_Kullanici_Istatistikleri_Getir
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- PaymentStatus = 'Paid' olan rezervasyonları al (iptal edilmemiş)
    DECLARE @PaidReservations TABLE (
        ReservationID INT,
        TripID INT,
        PaymentStatus NVARCHAR(50)
    );
    
    INSERT INTO @PaidReservations
    SELECT ReservationID, TripID, PaymentStatus
    FROM app.Reservations
    WHERE UserID = @UserID 
      AND Status != 'Cancelled' 
      AND PaymentStatus = 'Paid'; -- Reservation tablosundaki PaymentStatus alanı
    
    -- Trip ID'leri
    DECLARE @TripIds TABLE (TripID INT);
    INSERT INTO @TripIds
    SELECT DISTINCT TripID
    FROM @PaidReservations;
    
    -- Reservation ID'leri
    DECLARE @ReservationIds TABLE (ReservationID INT);
    INSERT INTO @ReservationIds
    SELECT ReservationID
    FROM @PaidReservations;
    
    -- Payments tablosundan toplam harcama
    DECLARE @TotalFromPayments DECIMAL(18, 2) = 0;
    SELECT @TotalFromPayments = ISNULL(SUM(Amount), 0)
    FROM app.Payments
    WHERE ReservationID IN (SELECT ReservationID FROM @ReservationIds)
      AND Status = 'Completed'; -- Payments tablosundaki Status alanı
    
    -- Eğer Payments'da kayıt yoksa, Trip fiyatlarını kullan
    DECLARE @TotalFromTrips DECIMAL(18, 2) = 0;
    IF @TotalFromPayments = 0
    BEGIN
        SELECT @TotalFromTrips = ISNULL(SUM(T.Price), 0)
        FROM app.Trips T
        INNER JOIN @PaidReservations PR ON T.TripID = PR.TripID;
    END
    
    -- Toplam harcama
    DECLARE @ToplamHarcama DECIMAL(18, 2) = CASE 
        WHEN @TotalFromPayments > 0 THEN @TotalFromPayments
        ELSE @TotalFromTrips
    END;
    
    -- Ortalama fiyat
    DECLARE @OrtalamaFiyat DECIMAL(18, 2) = 0;
    IF EXISTS (SELECT 1 FROM app.Payments WHERE ReservationID IN (SELECT ReservationID FROM @ReservationIds) AND Status = 'Completed')
    BEGIN
        SELECT @OrtalamaFiyat = ISNULL(AVG(Amount), 0)
        FROM app.Payments
        WHERE ReservationID IN (SELECT ReservationID FROM @ReservationIds)
          AND Status = 'Completed'; -- Payments tablosundaki Status alanı
    END
    ELSE IF EXISTS (SELECT 1 FROM @TripIds)
    BEGIN
        SELECT @OrtalamaFiyat = ISNULL(AVG(Price), 0)
        FROM app.Trips
        WHERE TripID IN (SELECT TripID FROM @TripIds);
    END
    
    -- Seyahat sayıları
    DECLARE @ToplamSeyahat INT = 0;
    DECLARE @GelecekSeyahat INT = 0;
    DECLARE @GecmisSeyahat INT = 0;
    
    SELECT 
        @ToplamSeyahat = COUNT(*),
        @GelecekSeyahat = SUM(CASE WHEN DepartureDate >= CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END),
        @GecmisSeyahat = SUM(CASE WHEN DepartureDate < CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END)
    FROM app.Trips
    WHERE TripID IN (SELECT TripID FROM @TripIds);
    
    -- Sonuç döndür
    SELECT 
        @UserID AS UserID,
        @ToplamHarcama AS ToplamHarcama,
        @OrtalamaFiyat AS OrtalamaSeyahatFiyati,
        @ToplamSeyahat AS ToplamSeyahatSayisi,
        @GelecekSeyahat AS GelecekSeyahatSayisi,
        @GecmisSeyahat AS GecmisSeyahatSayisi,
        (SELECT COUNT(*) FROM app.Reservations WHERE UserID = @UserID AND Status != 'Cancelled') AS ToplamRezervasyonSayisi,
        GETDATE() AS SonGuncellemeTarihi;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 7. sp_Sirket_Istatistikleri_Getir
-- =============================================
PRINT '7. sp_Sirket_Istatistikleri_Getir oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Sirket_Istatistikleri_Getir', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Sirket_Istatistikleri_Getir;
GO

CREATE PROCEDURE [proc].sp_Sirket_Istatistikleri_Getir
    @SirketID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Şirket bilgileri
    DECLARE @SirketAdi NVARCHAR(255) = NULL;
    DECLARE @SirketEmail NVARCHAR(255) = NULL;
    
    SELECT 
        @SirketAdi = FullName,
        @SirketEmail = Email
    FROM app.Users
    WHERE UserID = @SirketID;
    
    -- Şirkete ait araç ID'leri
    DECLARE @CompanyVehicles TABLE (VehicleID INT);
    INSERT INTO @CompanyVehicles
    SELECT VehicleID
    FROM app.Vehicles
    WHERE CompanyID = @SirketID;
    
    -- Şirkete ait sefer ID'leri
    DECLARE @CompanyTripIds TABLE (TripID INT);
    INSERT INTO @CompanyTripIds
    SELECT T.TripID
    FROM app.Trips T
    INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID;
    
    -- İstatistikleri hesapla
    SELECT 
        @SirketID AS SirketID,
        @SirketAdi AS SirketAdi,
        @SirketEmail AS SirketEmail,
        
        -- Sefer İstatistikleri
        (SELECT COUNT(*) FROM app.Trips T INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID) AS TotalTrips,
        (SELECT COUNT(*) FROM app.Trips T INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID WHERE T.Status = 1) AS ActiveTrips,
        (SELECT COUNT(*) FROM app.Trips T INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID WHERE T.Status = 0) AS IptalSefer,
        
        -- Rezervasyon İstatistikleri
        (SELECT COUNT(*) FROM app.Reservations R INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID WHERE R.Status != 'Cancelled') AS TotalReservations,
        (SELECT COUNT(*) FROM app.Reservations R INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID WHERE R.Status IN ('Reserved', 'Confirmed')) AS ActiveReservations,
        (SELECT COUNT(*) FROM app.Reservations R INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID WHERE R.Status = 'Cancelled') AS IptalRezervasyon,
        
        -- Gelir İstatistikleri
        (SELECT ISNULL(SUM(P.Amount), 0)
         FROM app.Payments P
         INNER JOIN app.Reservations R ON P.ReservationID = R.ReservationID
         INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID
         WHERE P.Status = 'Completed') AS ToplamGelir,
        
        (SELECT ISNULL(SUM(P.Amount), 0)
         FROM app.Payments P
         INNER JOIN app.Reservations R ON P.ReservationID = R.ReservationID
         INNER JOIN @CompanyTripIds CT ON R.TripID = CT.TripID
         WHERE P.Status = 'Completed'
           AND P.PaymentDate >= DATEADD(MONTH, -1, GETDATE())) AS SonBirAyGelir,
        
        -- Araç İstatistikleri
        (SELECT COUNT(*) FROM app.Vehicles WHERE CompanyID = @SirketID AND Active = 1) AS ToplamArac,
        (SELECT COUNT(*) FROM app.Vehicles WHERE CompanyID = @SirketID AND Active = 1 AND VehicleType = 'Bus') AS OtobusSayisi,
        (SELECT COUNT(*) FROM app.Vehicles WHERE CompanyID = @SirketID AND Active = 1 AND VehicleType = 'Train') AS TrenSayisi,
        
        -- Dolu Koltuk Oranı
        (SELECT CASE 
            WHEN COUNT(*) > 0 THEN
                CAST(SUM(CASE WHEN TS.IsReserved = 1 THEN 1 ELSE 0 END) AS FLOAT) / 
                CAST(COUNT(*) AS FLOAT) * 100
            ELSE 0
         END
         FROM app.TripSeats TS
         INNER JOIN @CompanyTripIds CT ON TS.TripID = CT.TripID
         INNER JOIN app.Trips T ON TS.TripID = T.TripID
         WHERE T.Status = 1) AS OrtalamaDoluKoltukOrani,
        
        -- Bu Ay Eklenen Sefer
        (SELECT COUNT(*) 
         FROM app.Trips T
         INNER JOIN @CompanyVehicles CV ON T.VehicleID = CV.VehicleID
         WHERE T.CreatedAt >= DATEADD(MONTH, -1, GETDATE())) AS BuAyEklenenSefer,
        
        -- Son Güncelleme Tarihi
        GETDATE() AS SonGuncellemeTarihi;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 8. sp_Sirket_Seferleri_Getir
-- =============================================
PRINT '8. sp_Sirket_Seferleri_Getir oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Sirket_Seferleri_Getir', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Sirket_Seferleri_Getir;
GO

CREATE PROCEDURE [proc].sp_Sirket_Seferleri_Getir
    @SirketID INT
AS
BEGIN
    SELECT 
        T.TripID,
        V.PlateOrCode AS AracPlaka,
        C1.CityName + ' > ' + C2.CityName AS Guzergah,
        T.DepartureDate AS Tarih,
        T.DepartureTime AS Saat,
        T.Price AS Fiyat,
        
        CASE 
            WHEN T.Status = 1 THEN 'Aktif'
            ELSE 'İptal'
        END AS Durum,
        
        (SELECT COUNT(*) FROM app.TripSeats WHERE TripID = T.TripID AND IsReserved = 1) AS DoluKoltukSayisi,
        (SELECT COUNT(*) FROM app.Seats WHERE VehicleID = V.VehicleID) AS ToplamKoltuk,
        
        CASE 
            WHEN V.CompanyID IS NULL THEN 'Admin'
            WHEN V.CompanyID = @SirketID THEN 'Şirket'
            ELSE 'Diğer'
        END AS SeferTipi
        
    FROM app.Trips T
    INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
    INNER JOIN app.Cities C1 ON T.FromCityID = C1.CityID
    INNER JOIN app.Cities C2 ON T.ToCityID = C2.CityID
    WHERE V.CompanyID = @SirketID
    ORDER BY T.DepartureDate DESC, T.DepartureTime DESC;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 9. sp_Sirket_Sefer_Ekle
-- =============================================
PRINT '9. sp_Sirket_Sefer_Ekle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Sirket_Sefer_Ekle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Sirket_Sefer_Ekle;
GO

CREATE PROCEDURE [proc].sp_Sirket_Sefer_Ekle
    @SirketID INT,
    @NeredenID INT,
    @NereyeID INT,
    @AracID INT,
    @Tarih DATE,
    @Saat TIME,
    @Fiyat DECIMAL(10,2)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. GÜVENLİK: Araç senin mi?
        IF NOT EXISTS (SELECT 1 FROM app.Vehicles WHERE VehicleID = @AracID AND CompanyID = @SirketID)
        BEGIN
            THROW 50001, 'Yetkisiz işlem: Bu araç firmanıza ait değil.', 1;
        END

        -- 2. ÇAKIŞMA KONTROLÜ
        IF EXISTS (SELECT 1 FROM app.Trips WHERE VehicleID = @AracID AND DepartureDate = @Tarih AND ABS(DATEDIFF(HOUR, DepartureTime, @Saat)) < 4 AND Status = 1)
        BEGIN
            THROW 50002, 'Araç meşgul.', 1;
        END

        -- 3. SEFER EKLE
        INSERT INTO app.Trips (VehicleID, FromCityID, ToCityID, DepartureDate, DepartureTime, Price, Status, CreatedAt)
        VALUES (@AracID, @NeredenID, @NereyeID, @Tarih, @Saat, @Fiyat, 1, SYSUTCDATETIME());

        -- 4. LOGLAMA
        DECLARE @YeniSeferID INT = SCOPE_IDENTITY();
        INSERT INTO log.TripLogs (TripID, Action, NewValue, LogDate, Description)
        VALUES (@YeniSeferID, 'Create', CAST(@Fiyat AS NVARCHAR), SYSUTCDATETIME(), 'Oluşturan Şirket ID: ' + CAST(@SirketID AS NVARCHAR));

        SELECT 'Sefer başarıyla oluşturuldu.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 10. sp_Sirket_Sefer_Guncelle
-- =============================================
PRINT '10. sp_Sirket_Sefer_Guncelle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Sirket_Sefer_Guncelle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Sirket_Sefer_Guncelle;
GO

CREATE PROCEDURE [proc].sp_Sirket_Sefer_Guncelle
    @SirketID INT,
    @SeferID INT,
    @Fiyat DECIMAL(10,2) = NULL,
    @Tarih DATE = NULL,
    @Saat TIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. GÜVENLİK: Sefer senin mi?
        IF NOT EXISTS (
            SELECT 1 
            FROM app.Trips T
            INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
            WHERE T.TripID = @SeferID AND V.CompanyID = @SirketID
        )
        BEGIN
            THROW 50001, 'Yetkisiz işlem: Bu sefer firmanıza ait değil.', 1;
        END

        -- 2. GÜNCELLEME
        UPDATE app.Trips
        SET 
            Price = ISNULL(@Fiyat, Price),
            DepartureDate = ISNULL(@Tarih, DepartureDate),
            DepartureTime = ISNULL(@Saat, DepartureTime)
        WHERE TripID = @SeferID;

        SELECT 'Sefer başarıyla güncellendi.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 11. sp_Sirket_Sefer_Iptal
-- =============================================
PRINT '11. sp_Sirket_Sefer_Iptal oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Sirket_Sefer_Iptal', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Sirket_Sefer_Iptal;
GO

CREATE PROCEDURE [proc].sp_Sirket_Sefer_Iptal
    @SirketID INT,
    @SeferID INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. GÜVENLİK: Sefer senin mi?
        IF NOT EXISTS (
            SELECT 1 
            FROM app.Trips T
            INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
            WHERE T.TripID = @SeferID AND V.CompanyID = @SirketID
        )
        BEGIN
            THROW 50001, 'Yetkisiz işlem: Bu sefer firmanıza ait değil.', 1;
        END

        -- 2. TARİH KONTROLÜ: Geçmiş seferler iptal edilemez
        IF EXISTS (SELECT 1 FROM app.Trips WHERE TripID = @SeferID AND DepartureDate < CAST(GETDATE() AS DATE))
        BEGIN
            THROW 50002, 'Geçmiş seferler iptal edilemez.', 1;
        END

        -- 3. İPTAL
        UPDATE app.Trips
        SET Status = 0
        WHERE TripID = @SeferID;

        SELECT 'Sefer başarıyla iptal edildi.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 12. sp_Admin_Kullanicilari_Getir
-- =============================================
PRINT '12. sp_Admin_Kullanicilari_Getir oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Admin_Kullanicilari_Getir', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Admin_Kullanicilari_Getir;
GO

CREATE PROCEDURE [proc].sp_Admin_Kullanicilari_Getir
    @AramaMetni NVARCHAR(50) = NULL,
    @RolID INT = NULL
AS
BEGIN
    SELECT 
        U.UserID,
        U.FullName,
        U.Email,
        U.Phone,
        R.RoleName,
        U.Status AS Durum,
        U.CreatedAt AS KayitTarihi,
        (SELECT ISNULL(SUM(Amount), 0) 
         FROM app.Payments P 
         INNER JOIN app.Reservations Res ON P.ReservationID = Res.ReservationID 
         WHERE Res.UserID = U.UserID) AS ToplamHarcama
    FROM app.Users U
    INNER JOIN app.Roles R ON U.RoleID = R.RoleID
    WHERE 
        (@RolID IS NULL OR U.RoleID = @RolID)
        AND
        (@AramaMetni IS NULL OR (U.FullName LIKE '%' + @AramaMetni + '%' OR U.Email LIKE '%' + @AramaMetni + '%'))
    ORDER BY U.CreatedAt DESC;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 13. sp_Admin_Kullanici_Durum_Degistir
-- =============================================
PRINT '13. sp_Admin_Kullanici_Durum_Degistir oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Admin_Kullanici_Durum_Degistir', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Admin_Kullanici_Durum_Degistir;
GO

CREATE PROCEDURE [proc].sp_Admin_Kullanici_Durum_Degistir
    @UserID INT,
    @YeniDurum TINYINT,
    @Sebep NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        UPDATE app.Users
        SET Status = @YeniDurum
        WHERE UserID = @UserID;

        SELECT 'Kullanıcı durumu güncellendi.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 14. sp_Admin_Arac_Ekle
-- =============================================
PRINT '14. sp_Admin_Arac_Ekle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Admin_Arac_Ekle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Admin_Arac_Ekle;
GO

CREATE PROCEDURE [proc].sp_Admin_Arac_Ekle
    @PlakaNo NVARCHAR(50),
    @AracTipi NVARCHAR(20),
    @ToplamKoltuk INT,
    @SirketID INT = NULL 
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. KONTROL: Mükerrer Plaka Var mı?
        IF EXISTS (SELECT 1 FROM app.Vehicles WHERE PlateOrCode = @PlakaNo)
        BEGIN
            THROW 50001, 'Bu plaka zaten kayıtlı.', 1;
        END

        -- 2. ARAÇ EKLEME
        INSERT INTO app.Vehicles (VehicleType, PlateOrCode, CompanyID)
        VALUES (@AracTipi, @PlakaNo, @SirketID);

        DECLARE @YeniAracID INT = SCOPE_IDENTITY();

        -- 3. KOLTUKLARI OTOMATİK OLUŞTUR
        DECLARE @Sayac INT = 1;
        WHILE @Sayac <= @ToplamKoltuk
        BEGIN
            INSERT INTO app.Seats (VehicleID, SeatNo, SeatPosition, WagonID)
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

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 15. sp_Admin_Sefer_Ekle
-- =============================================
PRINT '15. sp_Admin_Sefer_Ekle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Admin_Sefer_Ekle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Admin_Sefer_Ekle;
GO

CREATE PROCEDURE [proc].sp_Admin_Sefer_Ekle
    @NeredenID INT,
    @NereyeID INT,
    @AracID INT,
    @Tarih DATE,
    @Saat TIME,
    @Fiyat DECIMAL(10,2),
    @KalkisTerminalID INT = NULL,
    @VarisTerminalID INT = NULL,
    @KalkisIstasyonID INT = NULL,
    @VarisIstasyonID INT = NULL,
    @VarisTarihi DATE = NULL,
    @VarisSaati TIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- 1. KONTROL: Çakışma Kontrolü
        IF EXISTS (
            SELECT 1 FROM app.Trips 
            WHERE VehicleID = @AracID 
              AND DepartureDate = @Tarih 
              AND ABS(DATEDIFF(HOUR, DepartureTime, @Saat)) < 4
              AND Status = 1
        )
        BEGIN
            THROW 50001, 'Seçilen araç belirtilen saat aralığında başka bir seferde görünüyor.', 1;
        END

        -- 2. EKLEME İŞLEMİ
        INSERT INTO app.Trips (
            VehicleID, 
            FromCityID, 
            ToCityID, 
            DepartureTerminalID,
            ArrivalTerminalID,
            DepartureStationID,
            ArrivalStationID,
            DepartureDate, 
            DepartureTime,
            ArrivalDate,
            ArrivalTime,
            Price, 
            Status, 
            CreatedAt
        )
        VALUES (
            @AracID, 
            @NeredenID, 
            @NereyeID,
            @KalkisTerminalID,
            @VarisTerminalID,
            @KalkisIstasyonID,
            @VarisIstasyonID,
            @Tarih, 
            @Saat,
            @VarisTarihi,
            @VarisSaati,
            @Fiyat, 
            1, 
            SYSUTCDATETIME()
        );

        -- 3. KOLTUKLARI OTOMATİK OLUŞTUR
        DECLARE @YeniSeferID INT = SCOPE_IDENTITY();
        
        INSERT INTO app.TripSeats (TripID, SeatID, IsReserved)
        SELECT @YeniSeferID, SeatID, 0
        FROM app.Seats
        WHERE VehicleID = @AracID;

        SELECT 'Sefer başarıyla planlandı. Koltuklar otomatik oluşturuldu.' AS Mesaj;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 16. sp_Admin_Sefer_Iptal
-- =============================================
PRINT '16. sp_Admin_Sefer_Iptal oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Admin_Sefer_Iptal', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Admin_Sefer_Iptal;
GO

CREATE PROCEDURE [proc].sp_Admin_Sefer_Iptal
    @SeferID INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        UPDATE app.Trips
        SET Status = 0
        WHERE TripID = @SeferID;

        SELECT 'Sefer başarıyla iptal edildi.' AS Mesaj;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 17. sp_Zaman_Asimi_Rezervasyonlar
-- =============================================
PRINT '17. sp_Zaman_Asimi_Rezervasyonlar oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Zaman_Asimi_Rezervasyonlar', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Zaman_Asimi_Rezervasyonlar;
GO

CREATE PROCEDURE [proc].sp_Zaman_Asimi_Rezervasyonlar
    @TimeoutMinutes INT = 15,
    @MaxCancellations INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CancelledCount INT = 0;
    DECLARE @ErrorMessage NVARCHAR(500);
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Zaman aşımına uğrayan rezervasyonları bul ve iptal et
        DECLARE @ReservationsToCancel TABLE (
            ReservationID INT,
            UserID INT,
            ReservationDate DATETIME2
        );
        
        INSERT INTO @ReservationsToCancel (ReservationID, UserID, ReservationDate)
        SELECT 
            R.ReservationID,
            R.UserID,
            R.ReservationDate
        FROM app.Reservations R
        LEFT JOIN app.Payments P ON R.ReservationID = P.ReservationID
        WHERE R.Status = 'Reserved'
          AND (P.Status IS NULL OR P.Status = 'Pending')
          AND DATEDIFF(MINUTE, R.ReservationDate, GETDATE()) >= @TimeoutMinutes
          AND NOT EXISTS (
              SELECT 1 
              FROM log.AutoCancellationLog ACL 
              WHERE ACL.ReservationID = R.ReservationID
          );
        
        -- Rezervasyonları iptal et
        UPDATE R
        SET 
            R.Status = 'Cancelled',
            R.CancelReasonID = (SELECT TOP 1 ReasonID FROM app.CancellationReasons WHERE ReasonText LIKE '%Zaman aşımı%' OR ReasonText LIKE '%Timeout%')
        FROM app.Reservations R
        INNER JOIN @ReservationsToCancel RTC ON R.ReservationID = RTC.ReservationID;
        
        -- Koltukları serbest bırak
        UPDATE TS
        SET TS.IsReserved = 0
        FROM app.TripSeats TS
        INNER JOIN app.Reservations R ON TS.TripID = R.TripID AND TS.SeatID = R.SeatID
        INNER JOIN @ReservationsToCancel RTC ON R.ReservationID = RTC.ReservationID;
        
        -- Log kayıtları oluştur
        INSERT INTO log.AutoCancellationLog (ReservationID, UserID, Reason, OriginalReservationDate, TimeoutMinutes)
        SELECT 
            ReservationID,
            UserID,
            'Ödeme zaman aşımı - Otomatik iptal edildi',
            ReservationDate,
            @TimeoutMinutes
        FROM @ReservationsToCancel;
        
        SET @CancelledCount = @@ROWCOUNT;
        
        COMMIT TRANSACTION;
        
        SELECT 
            @CancelledCount AS IptalEdilenSayisi,
            'Başarılı' AS Durum,
            CAST(GETDATE() AS NVARCHAR(50)) AS IslemTarihi;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        SET @ErrorMessage = ERROR_MESSAGE();
        
        SELECT 
            0 AS IptalEdilenSayisi,
            'Hata: ' + @ErrorMessage AS Durum,
            CAST(GETDATE() AS NVARCHAR(50)) AS IslemTarihi;
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 18. sp_Otomatik_Iptal_Ayarlari
-- =============================================
PRINT '18. sp_Otomatik_Iptal_Ayarlari oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Otomatik_Iptal_Ayarlari', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Otomatik_Iptal_Ayarlari;
GO

CREATE PROCEDURE [proc].sp_Otomatik_Iptal_Ayarlari
    @IslemTipi NVARCHAR(20) = 'GET',
    @TimeoutMinutes INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @IslemTipi = 'GET'
    BEGIN
        SELECT 
            15 AS TimeoutMinutes,
            'Aktif' AS Durum,
            'Otomatik iptal sistemi aktif' AS Aciklama;
    END
    ELSE IF @IslemTipi = 'SET' AND @TimeoutMinutes IS NOT NULL
    BEGIN
        SELECT 
            @TimeoutMinutes AS TimeoutMinutes,
            'Güncellendi' AS Durum,
            'Otomatik iptal süresi ' + CAST(@TimeoutMinutes AS NVARCHAR(10)) + ' dakika olarak ayarlandı' AS Aciklama;
    END
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 19. sp_Otomatik_Zam_Cursor
-- =============================================
PRINT '19. sp_Otomatik_Zam_Cursor oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Otomatik_Zam_Cursor', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Otomatik_Zam_Cursor;
GO

CREATE PROCEDURE [proc].sp_Otomatik_Zam_Cursor
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SeferID INT;
    DECLARE @MevcutFiyat DECIMAL(10,2);
    DECLARE @ToplamKoltuk INT;
    DECLARE @DoluKoltuk INT;
    DECLARE @DolulukOrani FLOAT;

    DECLARE cur_Fiyatlandirma CURSOR FOR
    SELECT TripID, Price 
    FROM app.Trips 
    WHERE DepartureDate >= CAST(GETDATE() AS DATE) AND Status = 1;

    OPEN cur_Fiyatlandirma;
    FETCH NEXT FROM cur_Fiyatlandirma INTO @SeferID, @MevcutFiyat;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @ToplamKoltuk = COUNT(*) FROM app.TripSeats WHERE TripID = @SeferID;
        SELECT @DoluKoltuk = COUNT(*) FROM app.TripSeats WHERE TripID = @SeferID AND IsReserved = 1;

        IF @ToplamKoltuk > 0
        BEGIN
            SET @DolulukOrani = CAST(@DoluKoltuk AS FLOAT) / CAST(@ToplamKoltuk AS FLOAT);

            IF @DolulukOrani > 0.80
            BEGIN
                UPDATE app.Trips
                SET Price = @MevcutFiyat * 1.10
                WHERE TripID = @SeferID;

                PRINT 'Sefer ID: ' + CAST(@SeferID AS NVARCHAR) + ' için zam yapıldı. Yeni Fiyat: ' + CAST(@MevcutFiyat * 1.10 AS NVARCHAR);
            END
        END

        FETCH NEXT FROM cur_Fiyatlandirma INTO @SeferID, @MevcutFiyat;
    END

    CLOSE cur_Fiyatlandirma;
    DEALLOCATE cur_Fiyatlandirma;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 20. sp_Bildirim_Gonder
-- =============================================
PRINT '20. sp_Bildirim_Gonder oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Bildirim_Gonder', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Bildirim_Gonder;
GO

CREATE PROCEDURE [proc].sp_Bildirim_Gonder
    @UserID INT,
    @NotificationType NVARCHAR(50),
    @NotificationMethod NVARCHAR(20) = 'Email',
    @Subject NVARCHAR(200),
    @Message NVARCHAR(MAX),
    @RelatedReservationID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @NotificationID INT;
    
    DECLARE @EmailEnabled BIT = 1;
    DECLARE @SMSEnabled BIT = 0;
    DECLARE @TypeEnabled BIT = 1;
    
    SELECT 
        @EmailEnabled = ISNULL(EmailNotifications, 1),
        @SMSEnabled = ISNULL(SMSNotifications, 0),
        @TypeEnabled = CASE @NotificationType
            WHEN 'Reservation' THEN ISNULL(ReservationNotifications, 1)
            WHEN 'Payment' THEN ISNULL(PaymentNotifications, 1)
            WHEN 'Cancellation' THEN ISNULL(CancellationNotifications, 1)
            WHEN 'Reminder' THEN ISNULL(ReminderNotifications, 1)
            ELSE 1
        END
    FROM log.UserNotificationPreferences
    WHERE UserID = @UserID;
    
    IF @TypeEnabled = 0
    BEGIN
        SELECT 
            -1 AS NotificationID,
            'Kullanıcı bu bildirim tipini devre dışı bırakmış' AS Mesaj;
        RETURN;
    END
    
    IF @NotificationMethod = 'Both' AND (@EmailEnabled = 0 OR @SMSEnabled = 0)
    BEGIN
        IF @EmailEnabled = 1
            SET @NotificationMethod = 'Email';
        ELSE IF @SMSEnabled = 1
            SET @NotificationMethod = 'SMS';
        ELSE
        BEGIN
            SELECT 
                -1 AS NotificationID,
                'Kullanıcının aktif bildirim yöntemi yok' AS Mesaj;
            RETURN;
        END
    END
    
    INSERT INTO log.NotificationQueue (
        UserID,
        NotificationType,
        NotificationMethod,
        Subject,
        Message,
        RelatedReservationID
    )
    VALUES (
        @UserID,
        @NotificationType,
        @NotificationMethod,
        @Subject,
        @Message,
        @RelatedReservationID
    );
    
    SET @NotificationID = SCOPE_IDENTITY();
    
    SELECT 
        @NotificationID AS NotificationID,
        'Bildirim kuyruğa eklendi' AS Mesaj;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 21. sp_Bildirim_Kuyrugu_Isle
-- =============================================
PRINT '21. sp_Bildirim_Kuyrugu_Isle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Bildirim_Kuyrugu_Isle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Bildirim_Kuyrugu_Isle;
GO

CREATE PROCEDURE [proc].sp_Bildirim_Kuyrugu_Isle
    @MaxProcessCount INT = 100
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@MaxProcessCount)
        NotificationID,
        UserID,
        NotificationType,
        NotificationMethod,
        Subject,
        Message,
        RelatedReservationID
    FROM log.NotificationQueue
    WHERE Status = 'Pending'
      AND RetryCount < 3
    ORDER BY CreatedAt ASC;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 22. sp_Bildirim_Durum_Guncelle
-- =============================================
PRINT '22. sp_Bildirim_Durum_Guncelle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Bildirim_Durum_Guncelle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Bildirim_Durum_Guncelle;
GO

CREATE PROCEDURE [proc].sp_Bildirim_Durum_Guncelle
    @NotificationID INT,
    @Status NVARCHAR(20),
    @ErrorMessage NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE log.NotificationQueue
    SET 
        Status = @Status,
        SentAt = CASE WHEN @Status = 'Sent' THEN GETDATE() ELSE SentAt END,
        RetryCount = RetryCount + 1,
        ErrorMessage = @ErrorMessage
    WHERE NotificationID = @NotificationID;
    
    SELECT 
        @NotificationID AS NotificationID,
        'Durum güncellendi' AS Mesaj;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 23. sp_Bilet_Bilgileri
-- =============================================
PRINT '23. sp_Bilet_Bilgileri oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Bilet_Bilgileri', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Bilet_Bilgileri;
GO

CREATE PROCEDURE [proc].sp_Bilet_Bilgileri
    @ReservationID INT = NULL,
    @TicketNumber NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @ReservationID IS NOT NULL
    BEGIN
        SELECT * 
        FROM report.vw_Bilet_Detay
        WHERE ReservationID = @ReservationID;
    END
    ELSE IF @TicketNumber IS NOT NULL
    BEGIN
        SELECT * 
        FROM report.vw_Bilet_Detay
        WHERE BiletNumarasi = @TicketNumber;
    END
    ELSE
    BEGIN
        RAISERROR('ReservationID veya TicketNumber parametresi gereklidir.', 16, 1);
    END
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 24. sp_Kullanici_Raporu
-- =============================================
PRINT '24. sp_Kullanici_Raporu oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Kullanici_Raporu', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Kullanici_Raporu;
GO

CREATE PROCEDURE [proc].sp_Kullanici_Raporu
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Genel İstatistikler
    SELECT 
        'Genel İstatistikler' AS RaporTipi,
        KullaniciAdi,
        ToplamHarcama,
        ToplamSeyahatSayisi,
        GelecekSeyahatSayisi,
        GecmisSeyahatSayisi,
        OrtalamaSeyahatFiyati,
        EnCokGidilenSehir,
        SonSeyahatTarihi,
        ToplamRezervasyonSayisi,
        IptalEdilenRezervasyonSayisi
    FROM report.vw_Kullanici_Istatistikleri
    WHERE UserID = @UserID;
    
    -- Son 10 Seyahat
    SELECT TOP 10
        R.ReservationID,
        T.DepartureDate AS SeferTarihi,
        T.DepartureTime AS SeferSaati,
        C1.CityName AS KalkisSehri,
        C2.CityName AS VarisSehri,
        P.Amount AS OdenenTutar,
        R.Status AS RezervasyonDurumu,
        R.ReservationDate AS RezervasyonTarihi
    FROM app.Reservations R
    INNER JOIN app.Trips T ON R.TripID = T.TripID
    INNER JOIN app.Cities C1 ON T.FromCityID = C1.CityID
    INNER JOIN app.Cities C2 ON T.ToCityID = C2.CityID
    LEFT JOIN app.Payments P ON R.ReservationID = P.ReservationID AND P.Status = 'Completed'
    WHERE R.UserID = @UserID
    ORDER BY T.DepartureDate DESC, T.DepartureTime DESC;
    
    -- Aylık Harcama (Son 12 Ay)
    SELECT 
        YEAR(P.PaymentDate) AS Yil,
        MONTH(P.PaymentDate) AS Ay,
        SUM(P.Amount) AS AylikHarcama,
        COUNT(DISTINCT R.ReservationID) AS AylikSeyahatSayisi
    FROM app.Payments P
    INNER JOIN app.Reservations R ON P.ReservationID = R.ReservationID
    WHERE R.UserID = @UserID
      AND P.Status = 'Completed'
      AND P.PaymentDate >= DATEADD(MONTH, -12, GETDATE())
    GROUP BY YEAR(P.PaymentDate), MONTH(P.PaymentDate)
    ORDER BY Yil DESC, Ay DESC;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 25. sp_Seferleri_Listele
-- =============================================
PRINT '25. sp_Seferleri_Listele oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Seferleri_Listele', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Seferleri_Listele;
GO

CREATE PROCEDURE [proc].sp_Seferleri_Listele
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
         FROM app.TripSeats TS 
         WHERE TS.TripID = T.TripID AND TS.IsReserved = 0) AS BosKoltukSayisi,

        -- Kalkış Noktası (Terminal veya İstasyon)
        COALESCE(DT.TerminalName, DS.StationName) AS KalkisNoktasi,
        
        -- Varış Noktası
        COALESCE(ArrTerm.TerminalName, ArrS.StationName) AS VarisNoktasi

    FROM app.Trips T
    INNER JOIN app.Cities F ON T.FromCityID = F.CityID
    INNER JOIN app.Cities TC ON T.ToCityID = TC.CityID
    INNER JOIN app.Vehicles V ON T.VehicleID = V.VehicleID
    
    -- Otobüs bilgisi
    LEFT JOIN app.Buses B ON V.VehicleID = B.BusID AND V.VehicleType = 'Bus'
    
    -- Tren bilgisi
    LEFT JOIN app.Trains TR ON V.VehicleID = TR.TrainID AND V.VehicleType = 'Train'
    
    -- Terminaller 
    LEFT JOIN app.Terminals DT ON T.DepartureTerminalID = DT.TerminalID
    LEFT JOIN app.Terminals ArrTerm ON T.ArrivalTerminalID = ArrTerm.TerminalID
    
    -- İstasyonlar
    LEFT JOIN app.Stations DS ON T.DepartureStationID = DS.StationID
    LEFT JOIN app.Stations ArrS ON T.ArrivalStationID = ArrS.StationID

    WHERE 
        T.FromCityID = @NeredenID 
        AND T.ToCityID = @NereyeID
        AND T.DepartureDate = @Tarih
        AND T.Status = 1
    ORDER BY T.DepartureTime ASC;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 26. sp_Sefer_Koltuk_Durumu
-- =============================================
PRINT '26. sp_Sefer_Koltuk_Durumu oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Sefer_Koltuk_Durumu', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Sefer_Koltuk_Durumu;
GO

CREATE PROCEDURE [proc].sp_Sefer_Koltuk_Durumu
    @SeferID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        TS.SeatID,
        S.SeatNo,          -- Koltuk Numarası (1A, 2B vb.)
        S.SeatPosition,    -- Cam Kenarı / Koridor
        TS.IsReserved,     -- 1: Dolu, 0: Boş
        R.PaymentStatus,   -- 'Pending', 'Paid', 'Refunded' veya NULL
        
        -- Eğer Tren ise Vagon numarasını da getir, Otobüs ise NULL gelir
        W.WagonNo AS VagonNo

    FROM app.TripSeats TS
    INNER JOIN app.Seats S ON TS.SeatID = S.SeatID
    LEFT JOIN app.Wagons W ON S.WagonID = W.WagonID
    LEFT JOIN app.Reservations R ON TS.TripID = R.TripID AND TS.SeatID = R.SeatID AND R.Status = 'Reserved'
    WHERE TS.TripID = @SeferID
    
    -- Listeyi önce Vagon numarasına (Trense), sonra Koltuk Numarasına göre sırala
    ORDER BY W.WagonNo, LEN(S.SeatNo), S.SeatNo;
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 27. sp_Admin_Arac_Guncelle
-- =============================================
PRINT '27. sp_Admin_Arac_Guncelle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Admin_Arac_Guncelle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Admin_Arac_Guncelle;
GO

CREATE PROCEDURE [proc].sp_Admin_Arac_Guncelle
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
        IF NOT EXISTS (SELECT 1 FROM app.Vehicles WHERE VehicleID = @AracID)
        BEGIN
            THROW 50001, 'Araç bulunamadı.', 1;
        END

        -- Plaka kontrolü (eğer değiştiriliyorsa)
        IF @PlakaNo IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM app.Vehicles WHERE PlateOrCode = @PlakaNo AND VehicleID != @AracID)
            BEGIN
                THROW 50002, 'Bu plaka/kod başka bir araç tarafından kullanılıyor.', 1;
            END
        END

        -- Şirket kontrolü (eğer değiştiriliyorsa)
        IF @SirketID IS NOT NULL AND @SirketID != -1
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM app.Users WHERE UserID = @SirketID AND RoleID = 3) -- 3 = Şirket rolü
            BEGIN
                THROW 50003, 'Geçersiz şirket ID.', 1;
            END
        END

        -- Güncelleme işlemi
        UPDATE app.Vehicles
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

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 28. sp_Admin_Sefer_Guncelle
-- =============================================
PRINT '28. sp_Admin_Sefer_Guncelle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Admin_Sefer_Guncelle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Admin_Sefer_Guncelle;
GO

CREATE PROCEDURE [proc].sp_Admin_Sefer_Guncelle
    @SeferID INT,
    @NeredenID INT = NULL,
    @NereyeID INT = NULL,
    @AracID INT = NULL,
    @Tarih DATE = NULL,
    @Saat TIME = NULL,
    @Fiyat DECIMAL(10,2) = NULL,
    @KalkisTerminalID INT = NULL,
    @VarisTerminalID INT = NULL,
    @KalkisIstasyonID INT = NULL,
    @VarisIstasyonID INT = NULL,
    @VarisTarihi DATE = NULL,
    @VarisSaati TIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Sefer var mı kontrol et
        IF NOT EXISTS (SELECT 1 FROM app.Trips WHERE TripID = @SeferID)
        BEGIN
            THROW 50001, 'Sefer bulunamadı.', 1;
        END

        -- Geçmiş sefer kontrolü (sadece tarih değiştiriliyorsa)
        IF @Tarih IS NOT NULL OR @Saat IS NOT NULL
        BEGIN
            DECLARE @MevcutTarih DATE;
            DECLARE @MevcutSaat TIME;
            DECLARE @YeniTarih DATE = @Tarih;
            DECLARE @YeniSaat TIME = @Saat;

            SELECT @MevcutTarih = DepartureDate, @MevcutSaat = DepartureTime
            FROM app.Trips
            WHERE TripID = @SeferID;

            IF @YeniTarih IS NULL SET @YeniTarih = @MevcutTarih;
            IF @YeniSaat IS NULL SET @YeniSaat = @MevcutSaat;

            -- Geçmiş tarihli sefer kontrolü
            IF CAST(@YeniTarih AS DATETIME) + CAST(@YeniSaat AS DATETIME) < GETDATE()
            BEGIN
                THROW 50002, 'Geçmiş tarihli bir sefer güncellenemez.', 1;
            END
        END

        -- Çakışma kontrolü (araç veya tarih/saat değiştiriliyorsa)
        IF @AracID IS NOT NULL OR @Tarih IS NOT NULL OR @Saat IS NOT NULL
        BEGIN
            DECLARE @KontrolAracID INT = @AracID;
            DECLARE @KontrolTarih DATE = @Tarih;
            DECLARE @KontrolSaat TIME = @Saat;

            SELECT 
                @KontrolAracID = ISNULL(@KontrolAracID, VehicleID),
                @KontrolTarih = ISNULL(@KontrolTarih, DepartureDate),
                @KontrolSaat = ISNULL(@KontrolSaat, DepartureTime)
            FROM app.Trips
            WHERE TripID = @SeferID;

            IF EXISTS (
                SELECT 1 FROM app.Trips 
                WHERE VehicleID = @KontrolAracID 
                  AND DepartureDate = @KontrolTarih 
                  AND ABS(DATEDIFF(HOUR, DepartureTime, @KontrolSaat)) < 4
                  AND Status = 1
                  AND TripID != @SeferID
            )
            BEGIN
                THROW 50003, 'Seçilen araç belirtilen saat aralığında başka bir seferde görünüyor.', 1;
            END
        END

        -- Şehir kontrolü
        IF @NeredenID IS NOT NULL AND @NereyeID IS NOT NULL
        BEGIN
            IF @NeredenID = @NereyeID
            BEGIN
                THROW 50004, 'Kalkış ve varış şehirleri aynı olamaz.', 1;
            END
        END

        -- Güncelleme işlemi
        UPDATE app.Trips
        SET 
            FromCityID = ISNULL(@NeredenID, FromCityID),
            ToCityID = ISNULL(@NereyeID, ToCityID),
            VehicleID = ISNULL(@AracID, VehicleID),
            DepartureTerminalID = CASE 
                WHEN @KalkisTerminalID = -1 THEN NULL
                WHEN @KalkisTerminalID IS NOT NULL THEN @KalkisTerminalID
                ELSE DepartureTerminalID
            END,
            ArrivalTerminalID = CASE 
                WHEN @VarisTerminalID = -1 THEN NULL
                WHEN @VarisTerminalID IS NOT NULL THEN @VarisTerminalID
                ELSE ArrivalTerminalID
            END,
            DepartureStationID = CASE 
                WHEN @KalkisIstasyonID = -1 THEN NULL
                WHEN @KalkisIstasyonID IS NOT NULL THEN @KalkisIstasyonID
                ELSE DepartureStationID
            END,
            ArrivalStationID = CASE 
                WHEN @VarisIstasyonID = -1 THEN NULL
                WHEN @VarisIstasyonID IS NOT NULL THEN @VarisIstasyonID
                ELSE ArrivalStationID
            END,
            DepartureDate = ISNULL(@Tarih, DepartureDate),
            DepartureTime = ISNULL(@Saat, DepartureTime),
            ArrivalDate = CASE 
                WHEN @VarisTarihi = '1900-01-01' THEN NULL
                WHEN @VarisTarihi IS NOT NULL THEN @VarisTarihi
                ELSE ArrivalDate
            END,
            ArrivalTime = CASE 
                WHEN @VarisSaati = '00:00:00' THEN NULL
                WHEN @VarisSaati IS NOT NULL THEN @VarisSaati
                ELSE ArrivalTime
            END,
            Price = ISNULL(@Fiyat, Price)
        WHERE TripID = @SeferID;

        SELECT 'Sefer bilgileri başarıyla güncellendi.' AS Mesaj;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- 29. sp_Admin_Kullanici_Guncelle
-- =============================================
PRINT '29. sp_Admin_Kullanici_Guncelle oluşturuluyor...';

IF OBJECT_ID('[proc].sp_Admin_Kullanici_Guncelle', 'P') IS NOT NULL
    DROP PROCEDURE [proc].sp_Admin_Kullanici_Guncelle;
GO

CREATE PROCEDURE [proc].sp_Admin_Kullanici_Guncelle
    @UserID INT,
    @FullName NVARCHAR(100) = NULL,
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Kullanıcı var mı kontrol et
        IF NOT EXISTS (SELECT 1 FROM app.Users WHERE UserID = @UserID)
        BEGIN
            THROW 50001, 'Kullanıcı bulunamadı.', 1;
        END

        -- Email kontrolü (eğer değiştiriliyorsa)
        IF @Email IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM app.Users WHERE Email = @Email AND UserID != @UserID)
            BEGIN
                THROW 50002, 'Bu email adresi başka bir kullanıcı tarafından kullanılıyor.', 1;
            END
        END

        -- Güncelleme işlemi (sadece NULL olmayan alanlar güncellenir)
        UPDATE app.Users
        SET 
            FullName = ISNULL(@FullName, FullName),
            Email = ISNULL(@Email, Email),
            Phone = ISNULL(@Phone, Phone)
        WHERE UserID = @UserID;

        SELECT 'Kullanıcı bilgileri başarıyla güncellendi.' AS Mesaj;

    END TRY
    BEGIN CATCH
        DECLARE @Hata NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@Hata, 16, 1);
    END CATCH
END;
GO

PRINT '   ✅ Tamamlandı';
PRINT '';

-- =============================================
-- TAMAMLANDI
-- =============================================
PRINT '========================================';
PRINT 'TÜM STORED PROCEDURE''LER BAŞARIYLA OLUŞTURULDU!';
PRINT '========================================';
PRINT '';
PRINT 'Oluşturulan Stored Procedure''ler (proc schema):';
PRINT '  1. sp_Kullanici_Kayit';
PRINT '  2. sp_Kullanici_Giris';
PRINT '  3. sp_Rezervasyon_Yap';
PRINT '  4. sp_Odeme_Tamamla';
PRINT '  5. sp_Kullanici_Biletleri';
PRINT '  6. sp_Kullanici_Istatistikleri_Getir';
PRINT '  7. sp_Sirket_Istatistikleri_Getir';
PRINT '  8. sp_Sirket_Seferleri_Getir';
PRINT '  9. sp_Sirket_Sefer_Ekle';
PRINT '  10. sp_Sirket_Sefer_Guncelle';
PRINT '  11. sp_Sirket_Sefer_Iptal';
PRINT '  12. sp_Admin_Kullanicilari_Getir';
PRINT '  13. sp_Admin_Kullanici_Durum_Degistir';
PRINT '  14. sp_Admin_Arac_Ekle';
PRINT '  15. sp_Admin_Sefer_Ekle';
PRINT '  16. sp_Admin_Sefer_Iptal';
PRINT '  17. sp_Zaman_Asimi_Rezervasyonlar';
PRINT '  18. sp_Otomatik_Iptal_Ayarlari';
PRINT '  19. sp_Otomatik_Zam_Cursor';
PRINT '  20. sp_Bildirim_Gonder';
PRINT '  21. sp_Bildirim_Kuyrugu_Isle';
PRINT '  22. sp_Bildirim_Durum_Guncelle';
PRINT '  23. sp_Bilet_Bilgileri';
PRINT '  24. sp_Kullanici_Raporu';
PRINT '  25. sp_Seferleri_Listele';
PRINT '  26. sp_Sefer_Koltuk_Durumu';
PRINT '  27. sp_Admin_Arac_Guncelle';
PRINT '  28. sp_Admin_Sefer_Guncelle';
PRINT '  29. sp_Admin_Kullanici_Guncelle';
PRINT '';
PRINT 'Test sorguları:';
PRINT '  EXEC [proc].sp_Kullanici_Kayit @AdSoyad = ''Test'', @Email = ''test@test.com'', ...;';
PRINT '  EXEC [proc].sp_Rezervasyon_Yap @SeferID = 1, @KoltukID = 1, ...;';
PRINT '';
GO

