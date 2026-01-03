-- =============================================
-- RayBus - Tüm View'lar
-- =============================================
-- Bu script tüm view'ları oluşturur

USE [RayBus]
GO

-- =============================================
-- 1. vw_Sefer_Detaylari
-- =============================================
-- Sefer detaylarını birleştiren view
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_Sefer_Detaylari')
    DROP VIEW vw_Sefer_Detaylari;
GO

CREATE VIEW vw_Sefer_Detaylari
AS
SELECT 
    T.TripID,
    T.DepartureDate,
    T.DepartureTime,
    
    -- Güzergah
    F.CityName AS Nereden,
    TC.CityName AS Nereye,
    
    -- Araç Bilgisi
    V.VehicleType AS AracTipi,
    V.PlateOrCode AS PlakaNo,
    
    -- Finansal
    T.Price AS BiletFiyati,
    
    -- Doluluk Durumu (Hesaplanmış Alan)
    (SELECT COUNT(*) FROM dbo.TripSeats TS WHERE TS.TripID = T.TripID) AS ToplamKoltuk,
    (SELECT COUNT(*) FROM dbo.TripSeats TS WHERE TS.TripID = T.TripID AND TS.IsReserved = 1) AS SatilanKoltuk,
    
    -- Durum
    CASE 
        WHEN T.Status = 1 THEN 'Aktif' 
        ELSE 'İptal' 
    END AS SeferDurumu

FROM dbo.Trips T
INNER JOIN dbo.Cities F ON T.FromCityID = F.CityID
INNER JOIN dbo.Cities TC ON T.ToCityID = TC.CityID
INNER JOIN dbo.Vehicles V ON T.VehicleID = V.VehicleID;
GO

-- =============================================
-- 2. vw_Guzergah_Ciro_Raporu
-- =============================================
-- Güzergah bazlı ciro raporu
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_Guzergah_Ciro_Raporu')
    DROP VIEW vw_Guzergah_Ciro_Raporu;
GO

CREATE VIEW vw_Guzergah_Ciro_Raporu
AS
SELECT 
    F.CityName + ' - ' + T.CityName AS Guzergah,
    V.VehicleType AS AracTipi,
    COUNT(R.ReservationID) AS ToplamSatisAdedi,
    ISNULL(SUM(P.Amount), 0) AS ToplamCiro,
    AVG(P.Amount) AS OrtalamaBiletFiyati

FROM dbo.Reservations R
INNER JOIN dbo.Trips Trip ON R.TripID = Trip.TripID
INNER JOIN dbo.Cities F ON Trip.FromCityID = F.CityID -- Nereden
INNER JOIN dbo.Cities T ON Trip.ToCityID = T.CityID -- Nereye
INNER JOIN dbo.Vehicles V ON Trip.VehicleID = V.VehicleID
LEFT JOIN dbo.Payments P ON R.ReservationID = P.ReservationID

WHERE R.Status <> 'Cancelled' -- İptal edilenleri ciroya katma
GROUP BY F.CityName, T.CityName, V.VehicleType;
GO

-- =============================================
-- 3. vw_Tum_Lokasyonlar_Union
-- =============================================
-- Terminaller ve istasyonları birleştiren view
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_Tum_Lokasyonlar_Union')
    DROP VIEW vw_Tum_Lokasyonlar_Union;
GO

CREATE VIEW vw_Tum_Lokasyonlar_Union
AS
SELECT 
    CityID,
    TerminalName AS YerAdi,
    'Otobüs Terminali' AS YerTipi,
    Address
FROM dbo.Terminals

UNION ALL -- İki tabloyu alt alta yapıştırır

SELECT 
    CityID,
    StationName AS YerAdi,
    'Tren Garı' AS YerTipi,
    Address
FROM dbo.Stations;
GO

-- =============================================
-- 4. vw_Admin_Dashboard_Ozet
-- =============================================
-- Admin dashboard için özet istatistikler
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_Admin_Dashboard_Ozet')
    DROP VIEW vw_Admin_Dashboard_Ozet;
GO

CREATE VIEW vw_Admin_Dashboard_Ozet
AS
SELECT 
    -- 1. Toplam Üye Sayısı
    (SELECT COUNT(*) FROM dbo.Users WHERE Status = 1) AS ToplamAktifUye,
    
    -- 2. Gelecekteki Aktif Sefer Sayısı
    (SELECT COUNT(*) FROM dbo.Trips WHERE DepartureDate >= CAST(GETDATE() AS DATE) AND Status = 1) AS GelecekSeferler,
    
    -- 3. Bugünün Toplam Cirosu (İptal edilmeyenler)
    (SELECT ISNULL(SUM(Amount), 0) 
     FROM dbo.Payments P 
     INNER JOIN dbo.Reservations R ON P.ReservationID = R.ReservationID
     WHERE R.Status = 'Reserved' 
       AND CAST(P.PaymentDate AS DATE) = CAST(GETDATE() AS DATE)) AS GunlukCiro,

    -- 4. Toplam Satılan Bilet (Tüm Zamanlar)
    (SELECT COUNT(*) FROM dbo.Reservations WHERE Status = 'Reserved') AS ToplamSatis,

    -- 5. Son 24 Saatteki Hata/Log Sayısı (Sistem Sağlığı İçin)
    (SELECT COUNT(*) FROM dbo.PaymentLogs WHERE LogDate >= DATEADD(day, -1, SYSUTCDATETIME())) AS SonIslemLoglari;
GO

-- =============================================
-- 5. vw_Admin_Gunluk_Finansal_Rapor
-- =============================================
-- Günlük finansal rapor (tarih ve ödeme yöntemi bazlı)
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_Admin_Gunluk_Finansal_Rapor')
    DROP VIEW vw_Admin_Gunluk_Finansal_Rapor;
GO

CREATE VIEW vw_Admin_Gunluk_Finansal_Rapor
AS
SELECT 
    -- Tarih bilgisini saatten arındırıp sadece GÜN yapıyoruz
    CAST(PaymentDate AS DATE) AS IslemTarihi,
    
    -- Kredi Kartı mı Havale mi? Kırılımı görmek için
    PaymentMethod AS OdemeYontemi,
    
    -- O gün kaç tane bilet satılmış?
    COUNT(*) AS ToplamSatisAdedi,
    
    -- O gün kasaya giren toplam para
    SUM(Amount) AS ToplamCiro
FROM dbo.Payments
WHERE Status = 'Completed' -- Sadece cebimize giren parayı sayıyoruz (İadeler hariç)
GROUP BY CAST(PaymentDate AS DATE), PaymentMethod;
-- (View içinde ORDER BY kullanılmaz, o yüzden select çekerken sıralayacağız)
GO

PRINT 'Tüm view''lar başarıyla oluşturuldu!';
GO

